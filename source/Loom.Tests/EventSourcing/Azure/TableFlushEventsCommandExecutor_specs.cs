namespace Loom.EventSourcing.Azure
{
    using System;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Loom.Json;
    using Loom.Messaging;
    using Loom.Testing;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;

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

        private TableFlushEventsCommandExecutor GenerateSut(IMessageBus eventBus) =>
            new TableFlushEventsCommandExecutor(Table, TypeResolver, JsonProcessor, eventBus);

        private TableEventStore<State1> GenerateEventStore(IMessageBus eventBus) =>
            new TableEventStore<State1>(Table, TypeResolver, JsonProcessor, eventBus);

        [TestMethod, AutoData]
        public void CanHandle_returns_true_for_FlushTableEvents_command_message(
            string commandId,
            FlushEvents command,
            TracingProperties tracingProperties,
            IMessageBus eventBus)
        {
            var message = new Message(id: commandId, data: command, tracingProperties);
            TableFlushEventsCommandExecutor sut = GenerateSut(eventBus);

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
            TableFlushEventsCommandExecutor sut = GenerateSut(eventBus);

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
            TableEventStore<State1> eventStore = GenerateEventStore(brokenEventBus);
            await TryCatchIgnore(() => eventStore.CollectEvents(streamId, startVersion, events));

            TableFlushEventsCommandExecutor sut = GenerateSut(eventBus);
            var command = new FlushEvents(TypeResolver.ResolveTypeName<State1>(), streamId);
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
            TableEventStore<State1> eventStore = GenerateEventStore(brokenEventBus);
            await TryCatchIgnore(() => eventStore.CollectEvents(streamId, startVersion, events));

            TableFlushEventsCommandExecutor sut = GenerateSut(eventBus);
            var command = new FlushEvents(TypeResolver.ResolveTypeName<State1>(), streamId);
            var message = new Message(id: commandId, data: command, tracingProperties);

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
