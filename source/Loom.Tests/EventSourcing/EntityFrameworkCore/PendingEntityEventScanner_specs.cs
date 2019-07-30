namespace Loom.EventSourcing.EntityFrameworkCore
{
    using System;
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
    public class PendingEntityEventScanner_specs
    {
        private SqliteConnection Connection { get; set; }

        private Func<EventStoreContext> ContextFactory { get; set; }

        private TypeResolver TypeResolver { get; } = new TypeResolver(
            new FullNameTypeNameResolvingStrategy(),
            new TypeResolvingStrategy());

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

        [TestMethod, AutoData]
        public async Task sut_sends_flush_commands_for_streams_containing_cold_pending_events(
            Guid streamId, long startVersion, Event1[] events, MessageBusDouble commandBus)
        {
            // Arrange
            var eventBus = new MessageBusDouble(errors: 1);
            var eventStore = new EntityEventStore<State1>(ContextFactory, TypeResolver, eventBus);
            await TryForget(() => eventStore.CollectEvents(streamId, startVersion, events));

            var sut = new PendingEntityEventScanner(ContextFactory, commandBus);

            // Act
            await sut.ScanPendingEvents();

            // Assert
            commandBus.Calls.Should().ContainSingle();
            (ImmutableArray<Message> messages, string partitionKey) = commandBus.Calls.Single();

            messages.Should().ContainSingle();
            Message message = messages.Single();

            Guid.TryParse(message.Id, out Guid id).Should().BeTrue();
            id.Should().NotBeEmpty();

            message.Data.Should().BeOfType<FlushEntityEvents>();
            message.Data.Should().BeEquivalentTo(new
            {
                StateType = TypeResolver.ResolveTypeName<State1>(),
                StreamId = streamId,
            });

            partitionKey.Should().Be($"{streamId}");
        }

        [TestMethod, AutoData]
        public async Task sut_sets_tracing_properties_correctly(
            Guid streamId, long startVersion, Event1[] events, MessageBusDouble commandBus)
        {
            // Arrange
            var eventBus = new MessageBusDouble(errors: 1);
            var eventStore = new EntityEventStore<State1>(ContextFactory, TypeResolver, eventBus);
            await TryForget(() => eventStore.CollectEvents(streamId, startVersion, events));

            var sut = new PendingEntityEventScanner(ContextFactory, commandBus);

            // Act
            await sut.ScanPendingEvents();

            // Assert
            TracingProperties actual = commandBus.Calls.Single().messages.Single().TracingProperties;
            actual.OperationId.Should().NotBeNullOrWhiteSpace();
            actual.Contributor.Should().Be("Loom.EventSourcing.EntityFrameworkCore.PendingEntityEventScanner");
            actual.ParentId.Should().BeNull();
        }

        [TestMethod, AutoData]
        public async Task sut_distincts_streams(
            Guid streamId, long startVersion, Event1[] events, MessageBusDouble commandBus)
        {
            // Arrange
            var eventBus = new MessageBusDouble(errors: 2);
            var eventStore = new EntityEventStore<State1>(ContextFactory, TypeResolver, eventBus);
            await TryForget(() => eventStore.CollectEvents(streamId, startVersion, events));
            await TryForget(() => eventStore.CollectEvents(streamId, startVersion + events.Length, events));

            var sut = new PendingEntityEventScanner(ContextFactory, commandBus);

            // Act
            await sut.ScanPendingEvents();

            // Assert
            commandBus.Calls.Should().ContainSingle();
        }

        [TestMethod, AutoData]
        public async Task sut_does_not_send_flush_command_for_streams_containing_only_hot_pending_events(
            Guid streamId, long startVersion, Event1[] events, MessageBusDouble commandBus)
        {
            // Arrange
            var eventBus = new MessageBusDouble(errors: 1);
            var eventStore = new EntityEventStore<State1>(ContextFactory, TypeResolver, eventBus);
            await TryForget(() => eventStore.CollectEvents(streamId, startVersion, events));

            var minimumPendingTime = TimeSpan.FromSeconds(1);
            var sut = new PendingEntityEventScanner(ContextFactory, commandBus, minimumPendingTime);

            // Act
            await sut.ScanPendingEvents();

            // Assert
            commandBus.Calls.Should().BeEmpty();
        }

        private static async Task TryForget(Func<Task> action)
        {
            try
            {
                await action.Invoke();
            }
            catch
            {
            }
        }
    }
}
