namespace Loom.EventSourcing.EntityFrameworkCore
{
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Data.Sqlite;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EventStoreContext_specs
    {
        public class EventStoreDbContext : EventStoreContext
        {
            public EventStoreDbContext(DbContextOptions options)
                : base(options)
            {
            }
        }

        private static SqliteConnection _connection;
        private static DbContextOptions _options;

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

        [TestMethod]
        public void sut_is_inheritable()
        {
            using var context = new EventStoreDbContext(_options);
            context.StreamEvents.Should().NotBeNull();
            context.PendingEvents.Should().NotBeNull();
            context.UniqueProperties.Should().NotBeNull();
        }
    }
}
