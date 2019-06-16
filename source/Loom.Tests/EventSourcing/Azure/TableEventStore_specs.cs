namespace Loom.EventSourcing.Azure
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;
    using AutoFixture;
    using FluentAssertions;
    using Loom.Messaging;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using static System.Guid;
    using static StorageEmulator;

    [TestClass]
    public class TableEventStore_specs
    {
        [TestMethod]
        public void sut_implements_IEventCollector()
        {
            typeof(TableEventStore).Should().Implement<IEventCollector>();
        }

        [TestMethod]
        public void sut_implements_IEventReader()
        {
            typeof(TableEventStore).Should().Implement<IEventReader>();
        }

        public class Event1
        {
            public Event1(int value) => Value = value;

            public int Value { get; }
        }

        public class Event2
        {
            public Event2(double value) => Value = value;

            public double Value { get; }
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

        private static TypeResolver TypeResolver =>
            new TypeResolver(
                new FullNameTypeNameResolvingStrategy(),
                new FullNameTypeResolvingStrategy());

        public static IEnumerable<object[]> Events
        {
            get
            {
                var builder = new Fixture();

                yield return new object[]
                {
                    new List<object>(
                        from e in new object[]
                        {
                            builder.Create<Event1>(),
                            builder.Create<Event2>(),
                            builder.Create<Event3>(),
                            builder.Create<Event4>(),
                        }
                        orderby e.GetHashCode()
                        select e),
                };
            }
        }

        [TestMethod]
        [DynamicData(nameof(Events), DynamicDataSourceType.Property)]
        public async Task QueryEvents_restores_events_correctly(
            List<object> events)
        {
            // Arrange
            var sut = new TableEventStore(EventStoreTable,
                                          TypeResolver,
                                          eventBus: Mock.Of<IMessageBus>());
            Guid streamId = NewGuid();
            await sut.CollectEvents(operationId: default,
                                    contributor: default,
                                    parentId: default,
                                    streamId,
                                    startVersion: 1,
                                    events);

            // Act
            IEnumerable<object> actual = await
                sut.QueryEvents(streamId, fromVersion: 1);

            // Assert
            actual.Should().BeEquivalentTo(events, c => c.WithStrictOrdering());
        }

        [TestMethod]
        [DynamicData(nameof(Events), DynamicDataSourceType.Property)]
        public async Task QueryEvents_filters_events_by_stream_id(
            List<object> events)
        {
            // Arrange
            var sut = new TableEventStore(EventStoreTable,
                                          TypeResolver,
                                          eventBus: Mock.Of<IMessageBus>());

            Guid streamId = NewGuid();
            int startVersion = 1;

            await sut.CollectEvents(operationId: default,
                                    contributor: default,
                                    parentId: default,
                                    streamId,
                                    startVersion,
                                    events);

            await sut.CollectEvents(operationId: default,
                                    contributor: default,
                                    parentId: default,
                                    streamId: NewGuid(),
                                    startVersion,
                                    events);

            // Act
            IEnumerable<object> actual = await
                sut.QueryEvents(streamId, fromVersion: 1);

            // Assert
            actual.Should().BeEquivalentTo(events, c => c.WithStrictOrdering());
        }

        [TestMethod]
        [DynamicData(nameof(Events), DynamicDataSourceType.Property)]
        public async Task QueryEvents_filters_events_by_version(
            List<object> events)
        {
            // Arrange
            var sut = new TableEventStore(EventStoreTable,
                                          TypeResolver,
                                          eventBus: Mock.Of<IMessageBus>());
            Guid streamId = NewGuid();
            await sut.CollectEvents(operationId: default,
                                    contributor: default,
                                    parentId: default,
                                    streamId,
                                    startVersion: 1,
                                    events);

            // Act
            IEnumerable<object> actual = await
                sut.QueryEvents(streamId, fromVersion: 2);

            // Assert
            actual.Should().BeEquivalentTo(events.Skip(1),
                                           c => c.WithStrictOrdering());
        }

        [TestMethod]
        public async Task CollectEvents_controls_concurrency()
        {
            // Arrange
            var sut = new TableEventStore(EventStoreTable,
                                          TypeResolver,
                                          eventBus: Mock.Of<IMessageBus>());
            Guid streamId = NewGuid();
            int version = 1;
            object[] events = new[] { new Event4(NewGuid()) };
            await sut.CollectEvents(operationId: default,
                                    contributor: default,
                                    parentId: default,
                                    streamId,
                                    startVersion: version,
                                    events);

            // Act
            Func<Task> action = () => sut.CollectEvents(operationId: default,
                                                        contributor: default,
                                                        parentId: default,
                                                        streamId,
                                                        startVersion: version,
                                                        events);

            // Assert
            await action.Should().ThrowAsync<StorageException>();
        }

        [TestMethod]
        public async Task CollectEvents_sends_messages_correctly()
        {
            // Arrange
            var spy = new MessageBusSpy();
            var sut = new TableEventStore(
                EventStoreTable, TypeResolver, eventBus: spy);

            var builder = new Fixture();

            string operationId = builder.Create<string>();
            string contributor = builder.Create<string>();
            string parentId = builder.Create<string>();
            Guid streamId = NewGuid();
            int startVersion = builder.Create<int>();
            Event1 event1 = builder.Create<Event1>();
            Event2 event2 = builder.Create<Event2>();
            Event3 event3 = builder.Create<Event3>();
            Event4 event4 = builder.Create<Event4>();
            object[] events = new object[] { event1, event2, event3, event4 };

            // Act
            await sut.CollectEvents(operationId,
                                    contributor,
                                    parentId,
                                    streamId,
                                    startVersion,
                                    events);

            // Assert
            spy.Calls.Should().ContainSingle();

            ImmutableArray<Message> call = spy.Calls.Single();

            call.Should()
                .HaveCount(events.Length)
                .And.OnlyContain(x => x.OperationId == operationId)
                .And.OnlyContain(x => x.Contributor == contributor)
                .And.OnlyContain(x => x.ParentId == parentId);

            VerifyData(call[0].Data, startVersion + 0, event1);
            VerifyData(call[1].Data, startVersion + 1, event2);
            VerifyData(call[2].Data, startVersion + 2, event3);
            VerifyData(call[3].Data, startVersion + 3, event4);

            void VerifyData<T>(object source,
                               long expectedVersion,
                               T expectedPayload)
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
            var spy = new MessageBusSpy();
            var sut = new TableEventStore(
                EventStoreTable, TypeResolver, eventBus: spy);

            Guid streamId = NewGuid();
            int version = new Fixture().Create<int>();

            var entity = new TableEntity($"{streamId}", $"{version:D19}");
            var operation = TableOperation.Insert(entity);
            await EventStoreTable.ExecuteAsync(operation);

            // Act
            Func<Task> action = () => sut.CollectEvents(operationId: default,
                                                        contributor: default,
                                                        parentId: default,
                                                        streamId,
                                                        version,
                                                        new[] { new object() });

            // Assert
            await action.Should().ThrowAsync<StorageException>();
            spy.Calls.Should().BeEmpty();
        }

        [TestMethod]
        public async Task if_CollectEvents_failed_to_send_messages_it_sends_them_next_time()
        {
            // Arrange
            IMessageBus stub = Mock.Of<IMessageBus>();
            var sut = new TableEventStore(
                EventStoreTable, TypeResolver, eventBus: stub);

            Guid streamId = NewGuid();
            int startVersion = new Fixture().Create<int>();

            var builder = new Fixture();
            Event1 event1 = builder.Create<Event1>();
            Event2 event2 = builder.Create<Event2>();
            Event3 event3 = builder.Create<Event3>();
            Event4 event4 = builder.Create<Event4>();

            Mock.Get(stub)
                .Setup(x => x.Send(It.IsAny<IEnumerable<Message>>()))
                .ThrowsAsync(new InvalidOperationException());

            try
            {
                await sut.CollectEvents(operationId: default,
                                        contributor: default,
                                        parentId: default,
                                        streamId,
                                        startVersion,
                                        new object[] { event1, event2 });
            }
            catch
            {
            }

            var spy = new MessageBusSpy();
            Mock.Get(stub)
                .Setup(x => x.Send(It.IsAny<IEnumerable<Message>>()))
                .Callback<IEnumerable<Message>>(x => spy.Send(x))
                .Returns(Task.CompletedTask);

            // Act
            await sut.CollectEvents(operationId: default,
                                    contributor: default,
                                    parentId: default,
                                    streamId,
                                    startVersion + 2,
                                    new object[] { event3, event4 });

            // Assert
            var calls = spy.Calls.ToImmutableArray();

            calls.Should().HaveCount(2);

            calls[0].Select(x => x.Data).Should().BeEquivalentTo(new object[]
            {
                new StreamEvent<Event1>(streamId, startVersion + 0, event1),
                new StreamEvent<Event2>(streamId, startVersion + 1, event2),
            }, c => c.WithStrictOrdering());

            calls[1].Select(x => x.Data).Should().BeEquivalentTo(new object[]
            {
                new StreamEvent<Event3>(streamId, startVersion + 2, event3),
                new StreamEvent<Event4>(streamId, startVersion + 3, event4),
            }, c => c.WithStrictOrdering());
        }

        [TestMethod]
        public async Task CollectEvents_does_not_send_messages_again()
        {
            // Arrange
            var spy = new MessageBusSpy();
            var sut = new TableEventStore(
                EventStoreTable, TypeResolver, eventBus: spy);

            Guid streamId = NewGuid();
            int startVersion = new Fixture().Create<int>();

            var builder = new Fixture();
            Event1 event1 = builder.Create<Event1>();
            Event2 event2 = builder.Create<Event2>();
            Event3 event3 = builder.Create<Event3>();
            Event4 event4 = builder.Create<Event4>();

            // Act
            await sut.CollectEvents(operationId: default,
                                    contributor: default,
                                    parentId: default,
                                    streamId,
                                    startVersion,
                                    new object[] { event1, event2 });

            await sut.CollectEvents(operationId: default,
                                    contributor: default,
                                    parentId: default,
                                    streamId,
                                    startVersion + 2,
                                    new object[] { event3, event4 });

            // Assert
            var calls = spy.Calls.ToImmutableArray();

            calls.Should().HaveCount(2);

            calls[0].Select(x => x.Data).Should().BeEquivalentTo(new object[]
            {
                new StreamEvent<Event1>(streamId, startVersion + 0, event1),
                new StreamEvent<Event2>(streamId, startVersion + 1, event2),
            }, c => c.WithStrictOrdering());

            calls[1].Select(x => x.Data).Should().BeEquivalentTo(new object[]
            {
                new StreamEvent<Event3>(streamId, startVersion + 2, event3),
                new StreamEvent<Event4>(streamId, startVersion + 3, event4),
            }, c => c.WithStrictOrdering());
        }

        [TestMethod]
        public async Task CollectEvents_sets_message_id_properties_to_unique_values()
        {
            // Arrange
            var spy = new MessageBusSpy();
            var sut = new TableEventStore(
                EventStoreTable, TypeResolver, eventBus: spy);

            Guid streamId = NewGuid();
            int startVersion = new Fixture().Create<int>();

            var builder = new Fixture();
            Event1 event1 = builder.Create<Event1>();
            Event2 event2 = builder.Create<Event2>();
            Event3 event3 = builder.Create<Event3>();
            Event4 event4 = builder.Create<Event4>();

            // Act
            await sut.CollectEvents(operationId: default,
                                    contributor: default,
                                    parentId: default,
                                    streamId,
                                    startVersion,
                                    new object[] { event1, event2 });

            await sut.CollectEvents(operationId: default,
                                    contributor: default,
                                    parentId: default,
                                    streamId,
                                    startVersion + 2,
                                    new object[] { event3, event4 });

            spy.Calls.SelectMany(x => x).Select(x => x.Id).Should().OnlyHaveUniqueItems();
        }

        [TestMethod]
        public async Task CollectEvents_preserves_message_id()
        {
            // Arrange
            var messages = new ConcurrentQueue<Message>();
            IMessageBus stub = Mock.Of<IMessageBus>();
            Guid streamId = NewGuid();
            int startVersion = new Fixture().Create<int>();

            var sut = new TableEventStore(
                EventStoreTable, TypeResolver, eventBus: stub);

            var builder = new Fixture();
            Event1 event1 = builder.Create<Event1>();
            Event2 event2 = builder.Create<Event2>();

            // Act
            Mock.Get(stub)
                .Setup(x => x.Send(It.IsAny<IEnumerable<Message>>()))
                .Callback<IEnumerable<Message>>(x => x.ForEach(messages.Enqueue))
                .ThrowsAsync(new InvalidOperationException());

            try
            {
                await sut.CollectEvents(operationId: default,
                                        contributor: default,
                                        parentId: default,
                                        streamId,
                                        startVersion,
                                        new[] { event1 });
            }
            catch
            {
            }

            Mock.Get(stub)
                .Setup(x => x.Send(It.IsAny<IEnumerable<Message>>()))
                .Callback<IEnumerable<Message>>(x => x.ForEach(messages.Enqueue))
                .Returns(Task.CompletedTask);

            await sut.CollectEvents(operationId: default,
                                    contributor: default,
                                    parentId: default,
                                    streamId,
                                    startVersion + 2,
                                    new[] { event2 });

            // Assert
            messages.Should().HaveCount(3);
            messages.Take(2).Select(x => x.Id).Distinct().Should().ContainSingle();
        }
    }
}
