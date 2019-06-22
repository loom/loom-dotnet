namespace Loom.EventSourcing.EntityFrameworkCore
{
    using System.Threading.Tasks;
    using Loom.Messaging;
    using Microsoft.Data.Sqlite;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EntityFrameworkEventStore_specs : EventStoreUnitTests<EntityFrameworkEventStore>
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

        protected override EntityFrameworkEventStore GenerateEventStore(
            TypeResolver typeResolver, IMessageBus eventBus)
        {
            EventStoreContext factory() => new EventStoreContext(_options);
            return new EntityFrameworkEventStore(factory, typeResolver, eventBus);
        }
    }
}
