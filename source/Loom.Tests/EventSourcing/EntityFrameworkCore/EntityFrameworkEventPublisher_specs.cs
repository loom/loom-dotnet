namespace Loom.EventSourcing.EntityFrameworkCore
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Threading.Tasks;
    using AutoFixture;
    using FluentAssertions;
    using Loom.Messaging;
    using Loom.Testing;
    using Microsoft.Data.Sqlite;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EntityFrameworkEventPublisher_specs
    {
        private SqliteConnection Connection { get; set; }

        private Func<EventStoreContext> ContextFactory { get; set; }

        private TypeResolver TypeResolver { get; } = new TypeResolver(
            new FullNameTypeNameResolvingStrategy(),
            new FullNameTypeResolvingStrategy());

        [TestInitialize]
        public async Task TestInitialize()
        {
            Connection = new SqliteConnection("DataSource=:memory:");
            await Connection.OpenAsync();
            DbContextOptions options = new DbContextOptionsBuilder().UseSqlite(Connection).Options;
            ContextFactory = () => new EventStoreContext(options);
            using (EventStoreContext db = ContextFactory.Invoke())
            {
                await db.Database.EnsureCreatedAsync();
            }
        }

        [TestCleanup]
        public void TestCleanup() => Connection.Dispose();

        [TestMethod, AutoDataRepeat(10)]
        public async Task sut_publishes_cold_pending_events(
            Guid streamId, int startVersion, IEnumerable<Event1> events, TracingProperties tracingProperties)
        {
            // Arrange
            var eventBus = new MessageBusDouble(errors: 1);
            var eventStore = new EntityFrameworkEventStore<State1>(ContextFactory, TypeResolver, eventBus);
            try
            {
                await eventStore.CollectEvents(streamId, startVersion, events, tracingProperties);
            }
            catch
            {
            }

            List<(ImmutableArray<Message>, string)> expected = eventBus.Calls.ToList();
            eventBus.Clear();

            TimeSpan minimumPendingTime = TimeSpan.Zero;
            var sut = new EntityFrameworkEventPublisher(ContextFactory, TypeResolver, eventBus, minimumPendingTime);

            // Act
            await sut.PublishPendingEvents();

            // Assert
            eventBus.Calls.Should().BeEquivalentTo(expected);
        }

        [TestMethod, AutoData]
        public async Task sut_publishes_cold_pending_events_sequentially(
            Guid streamId, [Range(2, 10)] int transactions, IFixture builder)
        {
            // Arrange
            var eventBus = new MessageBusDouble(errors: transactions);
            var eventStore = new EntityFrameworkEventStore<State1>(ContextFactory, TypeResolver, eventBus);
            int startVersion = 1;
            for (int i = 0; i < transactions; i++)
            {
                IEnumerable<Event1> events = builder.CreateMany<Event1>();
                TracingProperties tracingProperties = builder.Create<TracingProperties>();
                try
                {
                    await eventStore.CollectEvents(streamId, startVersion, events, tracingProperties);
                }
                catch
                {
                }

                startVersion += events.Count();
            }

            eventBus.Clear();

            TimeSpan minimumPendingTime = TimeSpan.Zero;
            var sut = new EntityFrameworkEventPublisher(ContextFactory, TypeResolver, eventBus, minimumPendingTime);

            // Act
            await sut.PublishPendingEvents();

            // Assert
            eventBus.Calls.Should().HaveCount(transactions);

            eventBus.Calls
                    .SelectMany(x => x.messages)
                    .Select(x => (dynamic)x.Data)
                    .Select(x => (long)x.Version)
                    .Should()
                    .BeInAscendingOrder();
        }

        [TestMethod, AutoDataRepeat(10)]
        public async Task sut_does_not_publish_hot_pending_events(
            Guid streamId, int startVersion, IEnumerable<Event1> events, TracingProperties tracingProperties)
        {
            // Arrange
            var eventBus = new MessageBusDouble(errors: 1);
            var eventStore = new EntityFrameworkEventStore<State1>(ContextFactory, TypeResolver, eventBus);
            try
            {
                await eventStore.CollectEvents(streamId, startVersion, events, tracingProperties);
            }
            catch
            {
            }

            eventBus.Clear();

            var minimumPendingTime = TimeSpan.FromMilliseconds(1000);
            var sut = new EntityFrameworkEventPublisher(ContextFactory, TypeResolver, eventBus, minimumPendingTime);

            // Act
            await sut.PublishPendingEvents();

            // Assert
            eventBus.Calls.Should().BeEmpty();
        }
    }
}
