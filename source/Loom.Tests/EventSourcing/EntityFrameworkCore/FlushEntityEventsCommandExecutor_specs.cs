namespace Loom.EventSourcing.EntityFrameworkCore
{
    using System;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Loom.Messaging;
    using Loom.Testing;
    using Microsoft.Data.Sqlite;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class FlushEntityEventsCommandExecutor_specs
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

        [TestMethod]
        public void sut_implements_IMessageHandler()
        {
            typeof(FlushEntityEventsCommandExecutor).Should().Implement<IMessageHandler>();
        }

        [TestMethod, AutoData]
        public void CanHandle_returns_true_for_FlushEntityFrameworkEvents_command_message(
            string commandId,
            FlushEntityEvents command,
            TracingProperties tracingProperties,
            IMessageBus eventBus)
        {
            var message = new Message(id: commandId, data: command, tracingProperties);
            var sut = new FlushEntityEventsCommandExecutor(ContextFactory, TypeResolver, eventBus);

            bool actual = sut.CanHandle(message);

            actual.Should().BeTrue();
        }

        [TestMethod, AutoData]
        public void CanHandle_returns_false_for_non_FlushTableEvents_command_message(
            string id,
            object data,
            TracingProperties tracingProperties,
            IMessageBus eventBus)
        {
            var message = new Message(id, data, tracingProperties);
            var sut = new FlushEntityEventsCommandExecutor(ContextFactory, TypeResolver, eventBus);

            bool actual = sut.CanHandle(message);

            actual.Should().BeFalse();
        }

        [TestMethod, AutoData]
        public async Task Handle_publishes_all_pending_events(
            Guid streamId,
            long startVersion,
            Event1[] events,
            string commandId,
            TracingProperties tracingProperties,
            MessageBusDouble eventBus)
        {
            // Arrange
            var brokenEventBus = new MessageBusDouble(errors: 1);
            var eventStore = new EntityEventStore<State1>(ContextFactory, TypeResolver, brokenEventBus);
            await TryForget(() => eventStore.CollectEvents(streamId, startVersion, events));

            var sut = new FlushEntityEventsCommandExecutor(ContextFactory, TypeResolver, eventBus);
            var command = new FlushEntityEvents(TypeResolver.ResolveTypeName<State1>(), streamId);
            var message = new Message(id: commandId, data: command, tracingProperties);

            // Act
            await sut.Handle(message);

            // Assert
            eventBus.Calls.Should().BeEquivalentTo(brokenEventBus.Calls);
        }

        [TestMethod, AutoData]
        public async Task Handle_is_idempotent(
            Guid streamId,
            long startVersion,
            Event1[] events,
            string commandId,
            TracingProperties tracingProperties,
            MessageBusDouble eventBus)
        {
            // Arrange
            var brokenEventBus = new MessageBusDouble(errors: 1);
            var eventStore = new EntityEventStore<State1>(ContextFactory, TypeResolver, brokenEventBus);
            await TryForget(() => eventStore.CollectEvents(streamId, startVersion, events));

            var sut = new FlushEntityEventsCommandExecutor(ContextFactory, TypeResolver, eventBus);
            var command = new FlushEntityEvents(TypeResolver.ResolveTypeName<State1>(), streamId);
            var message = new Message(id: commandId, data: command, tracingProperties);

            // Act
            await sut.Handle(message);
            await sut.Handle(message);

            // Assert
            eventBus.Calls.Should().BeEquivalentTo(brokenEventBus.Calls);
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
