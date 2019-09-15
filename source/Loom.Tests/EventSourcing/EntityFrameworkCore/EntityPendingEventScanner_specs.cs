namespace Loom.EventSourcing.EntityFrameworkCore
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Loom.Json;
    using Loom.Messaging;
    using Loom.Testing;
    using Microsoft.Data.Sqlite;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;

    [TestClass]
    public class EntityPendingEventScanner_specs
    {
        private SqliteConnection Connection { get; set; }

        private Func<EventStoreContext> ContextFactory { get; set; }

        private TypeResolver TypeResolver { get; } = new TypeResolver(
            new FullNameTypeNameResolvingStrategy(),
            new TypeResolvingStrategy());

        private IJsonProcessor JsonProcessor { get; } = new JsonProcessor(new JsonSerializer());

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

        private EntityEventStore<T> GenerateEventStore<T>(IMessageBus eventBus) =>
            new EntityEventStore<T>(ContextFactory, TypeResolver, JsonProcessor, eventBus);

        [TestMethod, AutoData]
        public async Task sut_sends_flush_commands_for_streams_containing_cold_pending_events(
            Guid streamId, long startVersion, Event1[] events, MessageBusDouble commandBus)
        {
            // Arrange
            var eventBus = new MessageBusDouble(errors: 1);
            EntityEventStore<State1> eventStore = GenerateEventStore<State1>(eventBus);
            await TryCatchIgnore(() => eventStore.CollectEvents(streamId, startVersion, events));

            var sut = new EntityPendingEventScanner(ContextFactory, commandBus);

            // Act
            await sut.ScanPendingEvents();

            // Assert
            commandBus.Calls.Should().ContainSingle();
            (ImmutableArray<Message> messages, string partitionKey) = commandBus.Calls.Single();

            messages.Should().ContainSingle();
            Message message = messages.Single();

            Guid.TryParse(message.Id, out Guid id).Should().BeTrue();
            id.Should().NotBeEmpty();

            message.Data.Should().BeOfType<FlushEvents>();
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
            EntityEventStore<State1> eventStore = GenerateEventStore<State1>(eventBus);
            await TryCatchIgnore(() => eventStore.CollectEvents(streamId, startVersion, events));

            var sut = new EntityPendingEventScanner(ContextFactory, commandBus);

            // Act
            await sut.ScanPendingEvents();

            // Assert
            TracingProperties actual = commandBus.Calls.Single().messages.Single().TracingProperties;
            actual.OperationId.Should().NotBeNullOrWhiteSpace();
            actual.Contributor.Should().Be("Loom.EventSourcing.EntityFrameworkCore.EntityPendingEventScanner");
            actual.ParentId.Should().BeNull();
        }

        [TestMethod, AutoData]
        public async Task sut_distincts_streams(
            Guid streamId, long startVersion, Event1[] events, MessageBusDouble commandBus)
        {
            // Arrange
            var eventBus = new MessageBusDouble(errors: 2);
            EntityEventStore<State1> eventStore = GenerateEventStore<State1>(eventBus);
            await TryCatchIgnore(() => eventStore.CollectEvents(streamId, startVersion, events));
            await TryCatchIgnore(() => eventStore.CollectEvents(streamId, startVersion + events.Length, events));

            var sut = new EntityPendingEventScanner(ContextFactory, commandBus);

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
            EntityEventStore<State1> eventStore = GenerateEventStore<State1>(eventBus);
            await TryCatchIgnore(() => eventStore.CollectEvents(streamId, startVersion, events));

            var minimumPendingTime = TimeSpan.FromSeconds(1);
            var sut = new EntityPendingEventScanner(ContextFactory, commandBus, minimumPendingTime);

            // Act
            await sut.ScanPendingEvents();

            // Assert
            commandBus.Calls.Should().BeEmpty();
        }

        private static async Task TryCatchIgnore(Func<Task> action)
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
