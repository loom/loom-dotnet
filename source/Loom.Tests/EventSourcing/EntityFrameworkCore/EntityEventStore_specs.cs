using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Loom.Messaging;
using Loom.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Loom.EventSourcing.EntityFrameworkCore
{
    [TestClass]
    public class EntityEventStore_specs :
        EventStoreUnitTests<EntityEventStore<State1>>
    {
        private static SqliteConnection _connection;
        private static DbContextOptions _options;

        public TestContext TestContext { get; set; }

        [ClassInitialize]
        [SuppressMessage("Usage", "CA1801:Review unused parameters", Justification = "Reviewed")]
        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Reviewed")]
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

        protected override EntityEventStore<State1> GenerateEventStore(
            TypeResolver typeResolver,
            IMessageBus eventBus)
        {
            return GenerateEventStore<State1>(typeResolver, eventBus);
        }

        private EntityEventStore<T> GenerateEventStore<T>(
            TypeResolver typeResolver,
            IMessageBus eventBus)
        {
            static EventStoreContext Factory() => new(_options);
            return new EntityEventStore<T>(Factory, typeResolver, JsonProcessor, eventBus);
        }

        private EntityEventStore<T> GenerateEventStore<T>(IMessageBus eventBus)
        {
            static EventStoreContext Factory() => new(_options);
            return new EntityEventStore<T>(Factory, TypeResolver, JsonProcessor, eventBus);
        }

        public EntityEventStore<State1> GenerateEventStore(
            IUniquePropertyDetector uniquePropertyDetector, IMessageBus eventBus)
        {
            return GenerateEventStore<State1>(uniquePropertyDetector, eventBus);
        }

        public EntityEventStore<T> GenerateEventStore<T>(
            IUniquePropertyDetector uniquePropertyDetector, IMessageBus eventBus)
        {
            static EventStoreContext Factory() => new(_options);
            return new EntityEventStore<T>(Factory, uniquePropertyDetector, TypeResolver, JsonProcessor, eventBus);
        }

        [TestMethod, AutoData]
        public async Task sut_supports_multiple_state_types_having_same_stream_id(
            IMessageBus eventBus, string streamId, Event1 evt1, Event2 evt2)
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
            actual1.Should().BeEquivalentTo(new[] { evt1 });

            IEnumerable<object> actual2 = await store2.QueryEvents(streamId, fromVersion: 1);
            actual2.Should().BeEquivalentTo(new[] { evt2 });
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
            string[] streams,
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
            foreach (string streamId in streams)
            {
                events.AddRange(await store.QueryEvents(streamId, fromVersion: 1));
            }

            events.Should().ContainSingle();
        }

        [TestMethod, AutoData]
        public async Task same_unique_value_is_allowed_for_different_properties(
            UniquePropertyDetector uniquePropertyDetector,
            IMessageBus eventBus,
            string stream1,
            string stream2,
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
            string stream,
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
            string stream1,
            string stream2,
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
            string stream1,
            string stream2,
            string value)
        {
            EntityEventStore<State1> store = GenerateEventStore<State1>(uniquePropertyDetector, eventBus);
            await store.CollectEvents(stream1, 1, new[] { new Event3(value) });
            await store.CollectEvents(stream1, 2, new[] { new Event3(null) });

            Func<Task> action = () => store.CollectEvents(stream2, 1, new[] { new Event3(value) });

            await action.Should().NotThrowAsync();
        }
    }
}
