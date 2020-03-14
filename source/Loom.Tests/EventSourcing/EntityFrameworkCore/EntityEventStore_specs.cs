namespace Loom.EventSourcing.EntityFrameworkCore
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Loom.Messaging;
    using Loom.Testing;
    using Microsoft.Data.Sqlite;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EntityEventStore_specs :
        EventStoreUnitTests<EntityEventStore<State1>>
    {
        private static SqliteConnection _connection;
        private static DbContextOptions _options;

        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static async Task ClassInitialize(TestContext context)
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            await _connection.OpenAsync();
            _options = new DbContextOptionsBuilder().UseSqlite(_connection).Options;
            using var db = new EventStoreContext(_options);
            await db.Database.EnsureCreatedAsync();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            _connection.Dispose();
        }

        protected override EntityEventStore<State1> GenerateEventStore(IMessageBus eventBus)
        {
            return GenerateEventStore<State1>(eventBus);
        }

        protected EntityEventStore<T> GenerateEventStore<T>(IMessageBus eventBus)
        {
            EventStoreContext factory() => new EventStoreContext(_options);
            return new EntityEventStore<T>(factory, TypeResolver, JsonProcessor, eventBus);
        }

        public EntityEventStore<State1> GenerateEventStore(
            IUniquePropertyDetector uniquePropertyDetector, IMessageBus eventBus)
        {
            return GenerateEventStore<State1>(uniquePropertyDetector, eventBus);
        }

        public EntityEventStore<T> GenerateEventStore<T>(
            IUniquePropertyDetector uniquePropertyDetector, IMessageBus eventBus)
        {
            EventStoreContext factory() => new EventStoreContext(_options);
            return new EntityEventStore<T>(factory, uniquePropertyDetector, TypeResolver, JsonProcessor, eventBus);
        }

        [TestMethod, AutoData]
        public async Task sut_supports_multiple_state_types_having_same_stream_id(
            IMessageBus eventBus, Guid streamId, Event1 evt1, Event2 evt2)
        {
            // Arrange
            EntityEventStore<State1> store1 = GenerateEventStore<State1>(eventBus);
            EntityEventStore<State2> store2 = GenerateEventStore<State2>(eventBus);

            int startVersion = 1;

            // Act
            Func<Task> action = async () =>
            {
                await store1.CollectEvents(streamId, startVersion, new[] { evt1 });
                await store2.CollectEvents(streamId, startVersion, new[] { evt2 });
            };

            // Assert
            await action.Should().NotThrowAsync();

            IEnumerable<object> actual1 = await store1.QueryEvents(streamId, fromVersion: 1);
            actual1.Should().BeEquivalentTo(evt1);

            IEnumerable<object> actual2 = await store2.QueryEvents(streamId, fromVersion: 1);
            actual2.Should().BeEquivalentTo(evt2);
        }

        public class UniquePropertyDetector : IUniquePropertyDetector
        {
            public IReadOnlyDictionary<string, string> GetUniqueProperties(object source) => source switch
            {
                Event1 e => new Dictionary<string, string> { ["Event1.Value"] = e.Value.ToString() },
                Event3 e => new Dictionary<string, string> { ["Event3.Value"] = e.Value },
                _ => ImmutableDictionary<string, string>.Empty,
            };
        }

        [TestMethod, AutoData]
        public async Task sut_supports_unique_constraint(
            UniquePropertyDetector uniquePropertyDetector,
            IMessageBus eventBus,
            Guid[] streams,
            Event3 evt)
        {
            // Arrange
            EntityEventStore<State1> store = GenerateEventStore<State1>(uniquePropertyDetector, eventBus);

            // Act
            try
            {
                await Task.WhenAll(streams.Select(streamId => store.CollectEvents(streamId, 1, new[] { evt })));
            }
            catch (Exception exception)
            {
                TestContext.WriteLine(exception.ToString());
            }

            // Assert
            var events = new List<object>();
            foreach (Guid streamId in streams)
            {
                events.AddRange(await store.QueryEvents(streamId, fromVersion: 1));
            }

            events.Should().ContainSingle();
        }

        [TestMethod, AutoData]
        public async Task same_unique_value_is_allowed_for_different_properties(
            UniquePropertyDetector uniquePropertyDetector,
            IMessageBus eventBus,
            Guid stream1,
            Guid stream2,
            int value)
        {
            EntityEventStore<State1> store = GenerateEventStore<State1>(uniquePropertyDetector, eventBus);
            await store.CollectEvents(stream1, 1, new[] { new Event1(value) });

            Func<Task> action = () => store.CollectEvents(stream2, 1, new[] { new Event3(value.ToString()) });

            await action.Should().NotThrowAsync();
        }

        [TestMethod, AutoData]
        public async Task same_unique_value_is_allowed_for_different_state_types(
            UniquePropertyDetector uniquePropertyDetector,
            IMessageBus eventBus,
            Guid stream,
            Event3 evt)
        {
            EntityEventStore<State1> store1 = GenerateEventStore<State1>(uniquePropertyDetector, eventBus);
            await store1.CollectEvents(stream, 1, new[] { evt });

            EntityEventStore<State2> store2 = GenerateEventStore<State2>(uniquePropertyDetector, eventBus);
            Func<Task> action = () => store2.CollectEvents(stream, 1, new[] { evt });

            await action.Should().NotThrowAsync();
        }

        [TestMethod, AutoData]
        public async Task replaced_unique_value_is_available(
            UniquePropertyDetector uniquePropertyDetector,
            IMessageBus eventBus,
            Guid stream1,
            Guid stream2,
            string value1,
            string value2)
        {
            EntityEventStore<State1> store = GenerateEventStore<State1>(uniquePropertyDetector, eventBus);
            await store.CollectEvents(stream1, 1, new[] { new Event3(value1) });
            await store.CollectEvents(stream1, 2, new[] { new Event3(value2) });

            Func<Task> action = () => store.CollectEvents(stream2, 1, new[] { new Event3(value1) });

            await action.Should().NotThrowAsync();
        }

        [TestMethod, AutoData]
        public async Task deleted_unique_value_is_available(
            UniquePropertyDetector uniquePropertyDetector,
            IMessageBus eventBus,
            Guid stream1,
            Guid stream2,
            string value)
        {
            EntityEventStore<State1> store = GenerateEventStore<State1>(uniquePropertyDetector, eventBus);
            await store.CollectEvents(stream1, 1, new[] { new Event3(value) });
            await store.CollectEvents(stream1, 2, new[] { new Event3(null) });

            Func<Task> action = () => store.CollectEvents(stream2, 1, new[] { new Event3(value) });

            await action.Should().NotThrowAsync();
        }

        [TestMethod, AutoData]
        public async Task QueryEventMessages_correctly_restores_event_messages(
            Guid streamId,
            Event1 evt1,
            Event2 evt2,
            Event3 evt3,
            IMessageBus eventBus,
            TracingProperties tracingProperties)
        {
            // Arrange
            EntityEventStore<State1> sut = GenerateEventStore(eventBus);
            object[] events = new object[] { evt1, evt2, evt3 };
            DateTime nowUtc = DateTime.UtcNow;
            await sut.CollectEvents(streamId, startVersion: 1, events, tracingProperties);

            // Act
            IEnumerable<Message> actual = await sut.QueryEventMessages(streamId);

            // Assert
            actual.Should().HaveSameCount(events);

            actual.Cast<dynamic>()
                  .Select(x => (Guid)x.Data.StreamId)
                  .Should().OnlyContain(x => x == streamId);

            actual.Cast<dynamic>()
                  .Select(x => (long)x.Data.Version)
                  .Should().BeEquivalentTo(new[] { 1, 2, 3 });

            actual.Cast<dynamic>()
                  .Select(x => (object)x.Data.Payload)
                  .Should().BeEquivalentTo(events);

            foreach (DateTime raisedTime in actual.Cast<dynamic>()
                                                  .Select(x => x.Data.RaisedTimeUtc))
            {
                raisedTime.Kind.Should().Be(DateTimeKind.Utc);
                raisedTime.Should().BeCloseTo(nowUtc, precision: 1000);
            }

            actual.ElementAt(0).Data.Should().BeOfType<StreamEvent<Event1>>();
            actual.ElementAt(1).Data.Should().BeOfType<StreamEvent<Event2>>();
            actual.ElementAt(2).Data.Should().BeOfType<StreamEvent<Event3>>();

            actual.Select(x => x.Id).Should().OnlyHaveUniqueItems();
            actual.Should().OnlyContain(x => x.TracingProperties == tracingProperties);
        }

        [TestMethod, AutoData]
        public async Task QueryEventMessages_returns_same_value_for_same_source(
            Guid streamId,
            Event1 evt1,
            Event2 evt2,
            Event3 evt3,
            IMessageBus eventBus,
            TracingProperties tracingProperties)
        {
            EntityEventStore<State1> sut = GenerateEventStore(eventBus);
            object[] events = new object[] { evt1, evt2, evt3 };
            await sut.CollectEvents(streamId, startVersion: 1, events, tracingProperties);

            IEnumerable<Message> actual = await sut.QueryEventMessages(streamId);

            actual.Should().BeEquivalentTo(await sut.QueryEventMessages(streamId));
        }
    }
}
