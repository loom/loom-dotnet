using System;
using System.Threading.Tasks;
using FluentAssertions;
using Loom.Json;
using Loom.Messaging;
using Loom.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Loom.EventSourcing.EntityFrameworkCore
{
    [TestClass]
    public class EntityFlushEventsCommandExecutor_specs
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
            using EventStoreContext db = ContextFactory.Invoke();
            await db.Database.EnsureCreatedAsync();
        }

        [TestMethod]
        public void sut_implements_IMessageHandler()
        {
            typeof(EntityFlushEventsCommandExecutor).Should().Implement<IMessageHandler>();
        }

        private EntityFlushEventsCommandExecutor GenerateSut(IMessageBus eventBus) =>
            new(ContextFactory, TypeResolver, JsonProcessor, eventBus);

        private EntityEventStore<T> GenerateEventStore<T>(IMessageBus eventBus) =>
            new(ContextFactory, TypeResolver, JsonProcessor, eventBus);

        [TestMethod, AutoData]
        public void sut_accepts_FlushEntityFrameworkEvents_command_message(
            string commandId,
            string processId,
            string initiator,
            string predecessorId,
            FlushEvents command,
            IMessageBus eventBus)
        {
            Message message = new(commandId, processId, initiator, predecessorId, data: command);
            EntityFlushEventsCommandExecutor sut = GenerateSut(eventBus);

            bool actual = sut.Accepts(message);

            actual.Should().BeTrue();
        }

        [TestMethod, AutoData]
        public void sut_does_not_accept_non_FlushTableEvents_command_message(
            Message message,
            IMessageBus eventBus)
        {
            EntityFlushEventsCommandExecutor sut = GenerateSut(eventBus);
            bool actual = sut.Accepts(message);
            actual.Should().BeFalse();
        }

        [TestMethod, AutoData]
        public async Task Handle_publishes_all_pending_events(
            string streamId,
            long startVersion,
            Event1[] events,
            string commandId,
            string processId,
            string initiator,
            string predecessorId,
            MessageBusDouble eventBus)
        {
            // Arrange
            var brokenEventBus = new MessageBusDouble(errors: 1);
            EntityEventStore<State1> eventStore = GenerateEventStore<State1>(brokenEventBus);
            await TryCatchIgnore(() => eventStore.CollectEvents(streamId, startVersion, events));

            EntityFlushEventsCommandExecutor sut = GenerateSut(eventBus);
            var command = new FlushEvents(TypeResolver.TryResolveTypeName<State1>(), streamId);
            Message message = new(commandId, processId, initiator, predecessorId, data: command);

            // Act
            await sut.Handle(message);

            // Assert
            eventBus.Calls.Should().BeEquivalentTo(brokenEventBus.Calls);
        }

        [TestMethod, AutoData]
        public async Task Handle_is_idempotent(
            string streamId,
            long startVersion,
            Event1[] events,
            string commandId,
            string processId,
            string initiator,
            string predecessorId,
            MessageBusDouble eventBus)
        {
            // Arrange
            var brokenEventBus = new MessageBusDouble(errors: 1);
            EntityEventStore<State1> eventStore = GenerateEventStore<State1>(brokenEventBus);
            await TryCatchIgnore(() => eventStore.CollectEvents(streamId, startVersion, events));

            EntityFlushEventsCommandExecutor sut = GenerateSut(eventBus);
            var command = new FlushEvents(TypeResolver.TryResolveTypeName<State1>(), streamId);
            Message message = new(commandId, processId, initiator, predecessorId, data: command);

            // Act
            await sut.Handle(message);
            await sut.Handle(message);

            // Assert
            eventBus.Calls.Should().BeEquivalentTo(brokenEventBus.Calls);
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
