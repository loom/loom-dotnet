namespace Loom.EventSourcing.EntityFrameworkCore
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Loom.Messaging;
    using Loom.Testing;
    using Microsoft.Data.Sqlite;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EntityFrameworkEventStore_specs :
        EventStoreUnitTests<EntityFrameworkEventStore<State1>>
    {
        private static SqliteConnection _connection;
        private static DbContextOptions _options;

        [ClassInitialize]
        public static async Task ClassInitialize(TestContext context)
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            await _connection.OpenAsync();
            _options = new DbContextOptionsBuilder().UseSqlite(_connection).Options;
            using (var db = new EventStoreContext(_options))
            {
                await db.Database.EnsureCreatedAsync();
            }
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            _connection.Dispose();
        }

        protected override EntityFrameworkEventStore<State1> GenerateEventStore(
            TypeResolver typeResolver, IMessageBus eventBus)
        {
            EventStoreContext factory() => new EventStoreContext(_options);
            return new EntityFrameworkEventStore<State1>(factory, typeResolver, eventBus);
        }

        [TestMethod, AutoData]
        public async Task sut_supports_multiple_state_types_having_same_stream_id(
            IMessageBus eventBus, Guid streamId, Event1 evt1, Event2 evt2)
        {
            // Arrange
            EventStoreContext factory() => new EventStoreContext(_options);

            var typeResolver = new TypeResolver(
                new FullNameTypeNameResolvingStrategy(),
                new FullNameTypeResolvingStrategy());

            var store1 = new EntityFrameworkEventStore<State1>(factory, typeResolver, eventBus);
            var store2 = new EntityFrameworkEventStore<State2>(factory, typeResolver, eventBus);

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
    }
}
