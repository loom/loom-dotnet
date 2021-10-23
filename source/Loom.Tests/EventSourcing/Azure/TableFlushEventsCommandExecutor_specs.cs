using System;
using System.Threading.Tasks;
using FluentAssertions;
using Loom.Json;
using Loom.Messaging;
using Loom.Testing;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Loom.EventSourcing.Azure
{
    [TestClass]
    public class TableFlushEventsCommandExecutor_specs
    {
        private CloudTable Table { get; } = StorageEmulator.EventStoreTable;

        private TypeResolver TypeResolver { get; } = new TypeResolver(
            new FullNameTypeNameResolvingStrategy(),
            new TypeResolvingStrategy());

        private IJsonProcessor JsonProcessor { get; } = new JsonProcessor(new JsonSerializer());

        [TestMethod]
        public void sut_implements_IMessageHandler()
        {
            typeof(TableFlushEventsCommandExecutor).Should().Implement<IMessageHandler>();
        }

        private TableFlushEventsCommandExecutor GenerateSut(IMessageBus eventBus)
            => new(Table, TypeResolver, JsonProcessor, eventBus);

        private TableEventStore<State1> GenerateEventStore(IMessageBus eventBus)
            => new(Table, TypeResolver, JsonProcessor, eventBus);

        [TestMethod, AutoData]
        public void sut_accepts_FlushTableEvents_command_message(
            string commandId,
            string processId,
            string initiator,
            string predecessorId,
            FlushEvents command,
            IMessageBus eventBus)
        {
            Message message = new(commandId, processId, initiator, predecessorId, data: command);
            TableFlushEventsCommandExecutor sut = GenerateSut(eventBus);

            bool actual = sut.Accepts(message);

            actual.Should().BeTrue();
        }

        [TestMethod, AutoData]
        public void sut_does_not_accept_non_FlushTableEvents_command_message(
            Message message,
            IMessageBus eventBus)
        {
            TableFlushEventsCommandExecutor sut = GenerateSut(eventBus);
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
            TableEventStore<State1> eventStore = GenerateEventStore(brokenEventBus);
            await TryCatchIgnore(() => eventStore.CollectEvents(streamId, startVersion, events));

            TableFlushEventsCommandExecutor sut = GenerateSut(eventBus);
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
            TableEventStore<State1> eventStore = GenerateEventStore(brokenEventBus);
            await TryCatchIgnore(() => eventStore.CollectEvents(streamId, startVersion, events));

            TableFlushEventsCommandExecutor sut = GenerateSut(eventBus);
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
