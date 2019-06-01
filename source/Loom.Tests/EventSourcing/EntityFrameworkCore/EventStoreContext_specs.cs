namespace Loom.EventSourcing.EntityFrameworkCore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AutoFixture;
    using FluentAssertions;
    using Microsoft.Data.Sqlite;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EventStoreContext_specs
    {
        [TestMethod]
        public void StreamEvent_entity_has_key_with_Sequence_property()
        {
            DbContextOptions options = new DbContextOptionsBuilder()
                .UseInMemoryDatabase(databaseName: $"{Guid.NewGuid()}")
                .Options;
            var sut = new EventStoreContext(options);
            IEntityType entity = sut.Model.FindEntityType(typeof(StreamEvent));
        }

        [TestMethod]
        public async Task Sequence_property_of_StreamEvent_is_database_generated()
        {
            DbContextOptions options = new DbContextOptionsBuilder()
                .UseInMemoryDatabase(databaseName: $"{Guid.NewGuid()}")
                .Options;

            using (var context = new EventStoreContext(options))
            {
                context.AddRange(new Fixture().CreateMany<StreamEvent>());
                await context.SaveChangesAsync();
            }

            using (var context = new EventStoreContext(options))
            {
                IQueryable<long> query =
                    from streamEvent in context.StreamEvents
                    orderby streamEvent.Sequence
                    select streamEvent.Sequence;

                List<long> actual = await query.ToListAsync();

                actual.Should().OnlyHaveUniqueItems().And.BeInAscendingOrder();
            }
        }

        [TestMethod]
        public async Task sut_has_guard_for_StreamEvent_against_duplicate_combination_of_StreamId_and_Version()
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

                var streamId = Guid.NewGuid();
                long version = new Fixture().Create<long>();

                // Act
                using (var context = new EventStoreContext(options))
                {
                    context.StreamEvents.Add(
                        new StreamEvent(
                            streamId, version, DateTime.UtcNow, "Empty", "{}"));
                    await context.SaveChangesAsync();
                }

                // Assert
                using (var context = new EventStoreContext(options))
                {
                    context.StreamEvents.Add(
                        new StreamEvent(
                            streamId, version, DateTime.UtcNow, "Empty", "{}"));
                    Func<Task> action = () => context.SaveChangesAsync();
                    action.Should().Throw<DbUpdateException>();
                }
            }
        }

        [TestMethod]
        public async Task sut_has_guard_for_StreamEvent_against_null_EventType()
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

                var streamEvent = new StreamEvent(
                    streamId: Guid.NewGuid(),
                    version: 1,
                    raisedTimeUtc: DateTime.UtcNow,
                    eventType: null,
                    payload: "{}");

                using (var context = new EventStoreContext(options))
                {
                    // Act
                    context.Add(streamEvent);
                    Func<Task> action = () => context.SaveChangesAsync();

                    // Assert
                    action.Should().Throw<DbUpdateException>();
                }
            }
        }

        [TestMethod]
        public async Task sut_has_guard_for_StreamEvent_against_null_Payload()
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

                var streamEvent = new StreamEvent(
                    streamId: Guid.NewGuid(),
                    version: 1,
                    raisedTimeUtc: DateTime.UtcNow,
                    eventType: "Empty",
                    payload: null);

                using (var context = new EventStoreContext(options))
                {
                    // Act
                    context.Add(streamEvent);
                    Func<Task> action = () => context.SaveChangesAsync();

                    // Assert
                    action.Should().Throw<DbUpdateException>();
                }
            }
        }
    }
}
