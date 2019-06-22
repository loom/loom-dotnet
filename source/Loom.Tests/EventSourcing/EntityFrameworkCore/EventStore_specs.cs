namespace Loom.EventSourcing.EntityFrameworkCore
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;
    using AutoFixture;
    using FluentAssertions;
    using FluentAssertions.Equivalency;
    using Loom.Messaging;
    using Microsoft.Data.Sqlite;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Newtonsoft.Json;
    using static System.Guid;

    [TestClass]
    public class EventStore_specs
    {
        public struct ComplexValue
        {
            public ComplexValue(int value1, string value2)
            {
                Value1 = value1;
                Value2 = value2;
            }

            public int Value1 { get; }

            public string Value2 { get; }
        }

        public class Event1
        {
            public Event1(int value1, string value2, ComplexValue value3)
            {
                Value1 = value1;
                Value2 = value2;
                Value3 = value3;
            }

            public int Value1 { get; }

            public string Value2 { get; }

            public ComplexValue Value3 { get; }
        }

        public class Event2
        {
            public Event2(int value1, string value2)
            {
                Value1 = value1;
                Value2 = value2;
            }

            public int Value1 { get; }

            public string Value2 { get; }
        }

        public class Event3
        {
            public Event3(string value) => Value = value;

            public string Value { get; }
        }

        public class Event4
        {
            public Event4(Guid value) => Value = value;

            public Guid Value { get; }
        }

        private static TypeResolver TypeResolver { get; } =
            new TypeResolver(
                new FullNameTypeNameResolvingStrategy(),
                new FullNameTypeResolvingStrategy());

        [TestMethod]
        public void sut_implements_IEventCollector()
        {
            typeof(EventStore).Should().Implement<IEventCollector>();
        }

        [TestMethod]
        public async Task given_single_event_then_CollectEvents_inserts_StreamEvent_entity_correctly()
        {
            // Arrange
            DbContextOptions options = new DbContextOptionsBuilder()
                .UseInMemoryDatabase(databaseName: $"{NewGuid()}")
                .Options;

            var sut = new EventStore(() => new EventStoreContext(options), TypeResolver, Mock.Of<IMessageBus>());

            Guid streamId = NewGuid();
            (Event1 evt, long version) = new Fixture().Create<(Event1, long)>();

            // Act
            await sut.CollectEvents(streamId, startVersion: version, events: new[] { evt });

            // Assert
            var context = new EventStoreContext(options);
            var query = from e in context.StreamEvents
                        where e.StreamId == streamId
                        select new { e.Version, e.EventType, e.Payload };

            var actual = await query.SingleOrDefaultAsync();

            actual.Should().BeEquivalentTo(new
            {
                Version = version,
                EventType = TypeResolver.ResolveTypeName<Event1>(),
                Payload = JsonConvert.SerializeObject(evt),
            });
        }

        [TestMethod]
        public async Task given_multiple_events_then_CollectEvents_inserts_StreamEvent_entities_correctly()
        {
            // Arrange
            DbContextOptions options = new DbContextOptionsBuilder()
                .UseInMemoryDatabase(databaseName: $"{NewGuid()}")
                .Options;

            var sut = new EventStore(() => new EventStoreContext(options), TypeResolver, Mock.Of<IMessageBus>());

            Guid streamId = NewGuid();
            (Event1 evt1, Event2 evt2, long startVersion) =
                new Fixture().Create<(Event1, Event2, long)>();

            // Act
            await sut.CollectEvents(streamId, startVersion, events: new object[] { evt1, evt2 });

            // Assert
            var context = new EventStoreContext(options);
            var query = from e in context.StreamEvents
                        where e.StreamId == streamId
                        orderby e.Version ascending
                        select new { e.Version, e.EventType, e.Payload };

            var actual = await query.ToListAsync();

            actual.Should().BeEquivalentTo(expectations: new object[]
            {
                new
                {
                    Version = startVersion,
                    EventType = TypeResolver.ResolveTypeName<Event1>(),
                    Payload = JsonConvert.SerializeObject(evt1),
                },
                new
                {
                    Version = startVersion + 1,
                    EventType = TypeResolver.ResolveTypeName<Event2>(),
                    Payload = JsonConvert.SerializeObject(evt2),
                },
            });
        }

        [TestMethod]
        public async Task CollectEvents_sets_RaisedTimeUtc_property_correctly()
        {
            // Arrange
            DbContextOptions options = new DbContextOptionsBuilder()
                .UseInMemoryDatabase(databaseName: $"{NewGuid()}")
                .Options;

            var sut = new EventStore(() => new EventStoreContext(options), TypeResolver, Mock.Of<IMessageBus>());

            Guid streamId = NewGuid();
            Event1 evt = new Fixture().Create<Event1>();

            DateTime nearby = DateTime.UtcNow;

            // Act
            await sut.CollectEvents(streamId, startVersion: 1, events: new[] { evt });

            // Assert
            var context = new EventStoreContext(options);
            IQueryable<DateTime> query = from e in context.StreamEvents
                                         where e.StreamId == streamId
                                         select e.RaisedTimeUtc;
            DateTime actual = await query.SingleOrDefaultAsync();
            actual.Kind.Should().Be(DateTimeKind.Utc);
            actual.Should().BeCloseTo(nearby, precision: 1000);
        }

        [TestMethod]
        public void sut_implements_IEventReader()
        {
            typeof(EventStore).Should().Implement<IEventReader>();
        }

        [TestMethod]
        public async Task QueryEvents_reads_event_stream_correctly()
        {
            // Arrange
            DbContextOptions options = new DbContextOptionsBuilder()
                .UseInMemoryDatabase(databaseName: $"{NewGuid()}")
                .Options;

            var sut = new EventStore(() => new EventStoreContext(options), TypeResolver, Mock.Of<IMessageBus>());

            Guid streamId = NewGuid();
            (Event1 evt1, Event2 evt2) = new Fixture().Create<(Event1, Event2)>();
            object[] events = new object[] { evt1, evt2 };

            await sut.CollectEvents(streamId, startVersion: 1, events);

            // Act
            IEnumerable<object> actual = await sut.QueryEvents(streamId, fromVersion: 1);

            // Assert
            actual.Should().BeEquivalentTo(events);
        }

        [TestMethod]
        public async Task QueryEvents_filters_events_by_version()
        {
            // Arrange
            DbContextOptions options = new DbContextOptionsBuilder()
                .UseInMemoryDatabase(databaseName: $"{NewGuid()}")
                .Options;

            var sut = new EventStore(() => new EventStoreContext(options), TypeResolver, Mock.Of<IMessageBus>());

            Guid streamId = NewGuid();
            (Event1 evt1, Event2 evt2) = new Fixture().Create<(Event1, Event2)>();
            object[] events = new object[] { evt1, evt2 };

            await sut.CollectEvents(streamId, startVersion: 1, events);

            // Act
            IEnumerable<object> actual = await sut.QueryEvents(streamId, fromVersion: 2);

            // Assert
            actual.Should().BeEquivalentTo(evt2);
        }

        [TestMethod]
        public async Task QueryEvents_filters_events_by_stream()
        {
            // Arrange
            DbContextOptions options = new DbContextOptionsBuilder()
                .UseInMemoryDatabase(databaseName: $"{NewGuid()}")
                .Options;

            var sut = new EventStore(() => new EventStoreContext(options), TypeResolver, Mock.Of<IMessageBus>());

            (Event1 evt1, Event2 evt2) = new Fixture().Create<(Event1, Event2)>();

            Guid streamId = NewGuid();
            await sut.CollectEvents(streamId, startVersion: 1, events: new[] { evt1 });

            var otherStreamId = Guid.NewGuid();
            await sut.CollectEvents(otherStreamId, startVersion: 2, events: new[] { evt2 });

            // Act
            IEnumerable<object> actual = await sut.QueryEvents(streamId, fromVersion: 1);

            // Assert
            actual.Should().BeEquivalentTo(evt1);
        }

        [TestMethod]
        public async Task QueryEvents_sorts_events_by_version()
        {
            // Arrange
            DbContextOptions options = new DbContextOptionsBuilder()
                .UseInMemoryDatabase(databaseName: $"{NewGuid()}")
                .Options;

            var sut = new EventStore(() => new EventStoreContext(options), TypeResolver, Mock.Of<IMessageBus>());

            Guid streamId = NewGuid();
            (Event1 evt1, Event2 evt2) = new Fixture().Create<(Event1, Event2)>();
            object[] events = new object[] { evt1, evt2 };

            await sut.CollectEvents(streamId, startVersion: 2, events: new[] { evt2 });
            await sut.CollectEvents(streamId, startVersion: 1, events: new[] { evt1 });

            // Act
            IEnumerable<object> actual = await sut.QueryEvents(streamId, fromVersion: 1);

            // Assert
            actual.Should().BeEquivalentTo(events, c => c.WithStrictOrdering());
        }

        [TestMethod]
        public async Task CollectEvents_controls_concurrency()
        {
            // Arrange
            using (var connection = new SqliteConnection("DataSource=:memory:"))
            {
                await connection.OpenAsync();

                DbContextOptions options = new DbContextOptionsBuilder()
                    .UseSqlite(connection)
                    .Options;

                using (var context = new EventStoreContext(options))
                {
                    await context.Database.EnsureCreatedAsync();
                }

                var sut = new EventStore(() => new EventStoreContext(options), TypeResolver, Mock.Of<IMessageBus>());

                Guid streamId = NewGuid();
                int version = 1;
                object[] events = new[] { new Fixture().Create<Event1>() };
                await sut.CollectEvents(streamId, startVersion: version, events);

                // Act
                Func<Task> action = () => sut.CollectEvents(streamId, startVersion: version, events);

                // Assert
                await action.Should().ThrowAsync<DbUpdateException>();
            }
        }

        [TestMethod]
        public async Task CollectEvents_sends_messages_correctly()
        {
            // Arrange
            DbContextOptions options = new DbContextOptionsBuilder()
                .UseInMemoryDatabase(databaseName: $"{NewGuid()}")
                .Options;

            var spy = new MessageBusSpy();
            var sut = new EventStore(() => new EventStoreContext(options), TypeResolver, eventBus: spy);

            var builder = new Fixture();

            Guid streamId = NewGuid();

            int startVersion = builder.Create<int>();

            Event1 event1 = builder.Create<Event1>();
            Event2 event2 = builder.Create<Event2>();
            Event3 event3 = builder.Create<Event3>();
            Event4 event4 = builder.Create<Event4>();

            object[] events = new object[] { event1, event2, event3, event4 };

            TracingProperties tracingProperties = builder.Create<TracingProperties>();

            // Act
            await sut.CollectEvents(streamId, startVersion, events, tracingProperties);

            // Assert
            spy.Calls.Should().ContainSingle();

            ImmutableArray<Message> call = spy.Calls.Single();

            call.Should()
                .HaveCount(events.Length)
                .And.OnlyContain(x => x.TracingProperties == tracingProperties);

            VerifyData(call[0].Data, startVersion + 0, event1);
            VerifyData(call[1].Data, startVersion + 1, event2);
            VerifyData(call[2].Data, startVersion + 2, event3);
            VerifyData(call[3].Data, startVersion + 3, event4);

            void VerifyData<T>(object source, long expectedVersion, T expectedPayload)
            {
                source.Should().BeOfType<StreamEvent<T>>();
                var data = (StreamEvent<T>)source;
                data.StreamId.Should().Be(streamId);
                data.Version.Should().Be(expectedVersion);
                data.Payload.Should().BeEquivalentTo(expectedPayload);
            }
        }

        [TestMethod]
        public async Task CollectEvents_does_not_send_messages_if_it_failed_to_save_events()
        {
            // Arrange
            using (var connection = new SqliteConnection("DataSource=:memory:"))
            {
                await connection.OpenAsync();

                DbContextOptions options = new DbContextOptionsBuilder()
                    .UseSqlite(connection)
                    .Options;

                using (var context = new EventStoreContext(options))
                {
                    await context.Database.EnsureCreatedAsync();
                }

                var spy = new MessageBusSpy();
                var sut = new EventStore(() => new EventStoreContext(options), TypeResolver, eventBus: spy);

                Guid streamId = NewGuid();
                int version = new Fixture().Create<int>();

                using (var context = new EventStoreContext(options))
                {
                    context.StreamEvents.Add(
                        new StreamEvent(
                            streamId,
                            version,
                            raisedTimeUtc: default,
                            eventType: string.Empty,
                            payload: string.Empty,
                            messageId: $"{NewGuid()}",
                            operationId: default,
                            contributor: default,
                            parentId: default,
                            transaction: NewGuid()));
                    await context.SaveChangesAsync();
                }

                // Act
                Func<Task> action = () => sut.CollectEvents(streamId, version, new[] { new object() });

                // Assert
                await action.Should().ThrowAsync<DbUpdateException>();
                spy.Calls.Should().BeEmpty();
            }
        }

        [TestMethod]
        public async Task if_CollectEvents_failed_to_send_messages_it_sends_them_next_time()
        {
            // Arrange
            IMessageBus stub = Mock.Of<IMessageBus>();
            var spy = new MessageBusSpy();

            var builder = new Fixture();
            Guid streamId = NewGuid();
            int startVersion = builder.Create<int>();
            Event1 evt1 = builder.Create<Event1>();
            Event2 evt2 = builder.Create<Event2>();
            Event3 evt3 = builder.Create<Event3>();
            Event4 evt4 = builder.Create<Event4>();

            using (var connection = new SqliteConnection("DataSource=:memory:"))
            {
                await connection.OpenAsync();

                DbContextOptions options = new DbContextOptionsBuilder()
                    .UseSqlite(connection)
                    .Options;

                using (var context = new EventStoreContext(options))
                {
                    await context.Database.EnsureCreatedAsync();
                }

                var sut = new EventStore(() => new EventStoreContext(options), TypeResolver, eventBus: stub);

                Mock.Get(stub)
                    .Setup(x => x.Send(It.IsAny<IEnumerable<Message>>()))
                    .ThrowsAsync(new InvalidOperationException());

                try
                {
                    await sut.CollectEvents(streamId, startVersion, new object[] { evt1, evt2 });
                }
                catch
                {
                }

                Mock.Get(stub)
                    .Setup(x => x.Send(It.IsAny<IEnumerable<Message>>()))
                    .Callback<IEnumerable<Message>>(x => spy.Send(x))
                    .Returns(Task.CompletedTask);

                // Act
                await sut.CollectEvents(streamId, startVersion + 2, new object[] { evt3, evt4 });
            }

            // Assert
            var calls = spy.Calls.ToImmutableArray();

            calls.Should().HaveCount(2);

            calls[0].Select(x => x.Data).Should().BeEquivalentTo(new object[]
            {
                new StreamEvent<Event1>(streamId, startVersion + 0, default, evt1),
                new StreamEvent<Event2>(streamId, startVersion + 1, default, evt2),
            }, c => c.WithStrictOrdering().Excluding((IMemberInfo m) => m.SelectedMemberInfo.Name == "RaisedTimeUtc"));

            calls[1].Select(x => x.Data).Should().BeEquivalentTo(new object[]
            {
                new StreamEvent<Event3>(streamId, startVersion + 2, default, evt3),
                new StreamEvent<Event4>(streamId, startVersion + 3, default, evt4),
            }, c => c.WithStrictOrdering().Excluding((IMemberInfo m) => m.SelectedMemberInfo.Name == "RaisedTimeUtc"));
        }

        [TestMethod]
        public async Task CollectEvents_does_not_send_messages_again()
        {
            // Arrange
            DbContextOptions options = new DbContextOptionsBuilder()
                .UseInMemoryDatabase(databaseName: $"{NewGuid()}")
                .Options;

            var spy = new MessageBusSpy();
            var sut = new EventStore(() => new EventStoreContext(options), TypeResolver, eventBus: spy);

            var builder = new Fixture();
            Guid streamId = NewGuid();
            int startVersion = builder.Create<int>();
            Event1 evt1 = builder.Create<Event1>();
            Event2 evt2 = builder.Create<Event2>();
            Event3 evt3 = builder.Create<Event3>();
            Event4 evt4 = builder.Create<Event4>();

            // Act
            await sut.CollectEvents(streamId, startVersion, new object[] { evt1, evt2 });
            await sut.CollectEvents(streamId, startVersion + 2, new object[] { evt3, evt4 });

            // Assert
            var calls = spy.Calls.ToImmutableArray();

            calls.Should().HaveCount(2);

            calls[0].Select(x => x.Data).Should().BeEquivalentTo(new object[]
            {
                new StreamEvent<Event1>(streamId, startVersion + 0, default, evt1),
                new StreamEvent<Event2>(streamId, startVersion + 1, default, evt2),
            }, c => c.WithStrictOrdering().Excluding((IMemberInfo m) => m.SelectedMemberInfo.Name == "RaisedTimeUtc"));

            calls[1].Select(x => x.Data).Should().BeEquivalentTo(new object[]
            {
                new StreamEvent<Event3>(streamId, startVersion + 2, default, evt3),
                new StreamEvent<Event4>(streamId, startVersion + 3, default, evt4),
            }, c => c.WithStrictOrdering().Excluding((IMemberInfo m) => m.SelectedMemberInfo.Name == "RaisedTimeUtc"));
        }

        [TestMethod]
        public async Task CollectEvents_sets_message_id_properties_to_unique_values()
        {
            // Arrange
            DbContextOptions options = new DbContextOptionsBuilder()
                .UseInMemoryDatabase(databaseName: $"{NewGuid()}")
                .Options;

            var spy = new MessageBusSpy();
            var sut = new EventStore(() => new EventStoreContext(options), TypeResolver, eventBus: spy);

            var builder = new Fixture();
            Guid streamId = NewGuid();
            int startVersion = builder.Create<int>();
            Event1 evt1 = builder.Create<Event1>();
            Event2 evt2 = builder.Create<Event2>();
            Event3 evt3 = builder.Create<Event3>();
            Event4 evt4 = builder.Create<Event4>();

            // Act
            await sut.CollectEvents(streamId, startVersion, new object[] { evt1, evt2 });
            await sut.CollectEvents(streamId, startVersion + 2, new object[] { evt3, evt4 });

            // Assert
            spy.Calls.SelectMany(x => x).Select(x => x.Id).Should().OnlyHaveUniqueItems();
        }

        [TestMethod]
        public async Task CollectEvents_preserves_message_id()
        {
            // Arrange
            DbContextOptions options = new DbContextOptionsBuilder()
                .UseInMemoryDatabase(databaseName: $"{NewGuid()}")
                .Options;

            var messages = new ConcurrentQueue<Message>();
            IMessageBus stub = Mock.Of<IMessageBus>();

            var sut = new EventStore(() => new EventStoreContext(options), TypeResolver, eventBus: stub);

            Guid streamId = NewGuid();
            var builder = new Fixture();
            int startVersion = builder.Create<int>();
            Event1 evt1 = builder.Create<Event1>();
            Event2 evt2 = builder.Create<Event2>();

            // Act
            Mock.Get(stub)
                .Setup(x => x.Send(It.IsAny<IEnumerable<Message>>()))
                .Callback<IEnumerable<Message>>(x => x.ForEach(messages.Enqueue))
                .ThrowsAsync(new InvalidOperationException());

            try
            {
                await sut.CollectEvents(streamId, startVersion, new[] { evt1 });
            }
            catch
            {
            }

            Mock.Get(stub)
                .Setup(x => x.Send(It.IsAny<IEnumerable<Message>>()))
                .Callback<IEnumerable<Message>>(x => x.ForEach(messages.Enqueue))
                .Returns(Task.CompletedTask);

            await sut.CollectEvents(streamId, startVersion + 1, new[] { evt2 });

            // Assert
            messages.Should().HaveCount(3);
            messages.Take(2).Select(x => x.Id).Distinct().Should().ContainSingle();
        }

        [TestMethod]
        public async Task CollectEvents_preserves_RaisedTimeUtc_property()
        {
            // Arrange
            DbContextOptions options = new DbContextOptionsBuilder()
                .UseInMemoryDatabase(databaseName: $"{NewGuid()}")
                .Options;

            var messages = new ConcurrentQueue<Message>();
            IMessageBus stub = Mock.Of<IMessageBus>();

            var sut = new EventStore(() => new EventStoreContext(options), TypeResolver, eventBus: stub);

            Guid streamId = NewGuid();
            var builder = new Fixture();
            int startVersion = builder.Create<int>();
            Event1 evt1 = builder.Create<Event1>();
            Event2 evt2 = builder.Create<Event2>();

            // Act
            Mock.Get(stub)
                .Setup(x => x.Send(It.IsAny<IEnumerable<Message>>()))
                .Callback<IEnumerable<Message>>(x => x.ForEach(messages.Enqueue))
                .ThrowsAsync(new InvalidOperationException());

            try
            {
                await sut.CollectEvents(streamId, startVersion, new[] { evt1 });
            }
            catch
            {
            }

            await Task.Delay(millisecondsDelay: 100);

            Mock.Get(stub)
                .Setup(x => x.Send(It.IsAny<IEnumerable<Message>>()))
                .Callback<IEnumerable<Message>>(x => x.ForEach(messages.Enqueue))
                .Returns(Task.CompletedTask);

            await sut.CollectEvents(streamId, startVersion + 1, new[] { evt2 });

            // Assert
            messages.Should()
                    .HaveCount(3)
                    .And
                    .Subject
                    .Take(2)
                    .Select(x => x.Data)
                    .Cast<StreamEvent<Event1>>()
                    .Select(x => x.RaisedTimeUtc)
                    .Distinct()
                    .Should()
                    .ContainSingle();
        }
    }
}
