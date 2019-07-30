namespace Loom.EventSourcing.Azure
{
    using System;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Loom.Messaging;
    using Loom.Testing;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class FlushTableEventsCommandExecutor_specs
    {
        private static CloudTable Table { get; } = StorageEmulator.EventStoreTable;

        private static TypeResolver TypeResolver { get; } = new TypeResolver(
            new FullNameTypeNameResolvingStrategy(),
            new TypeResolvingStrategy());

        [TestMethod]
        public void sut_implements_IMessageHandler()
        {
            typeof(FlushTableEventsCommandExecutor).Should().Implement<IMessageHandler>();
        }

        [TestMethod, AutoData]
        public void CanHandle_returns_true_for_FlushTableEvents_command_message(
            string commandId,
            FlushTableEvents command,
            TracingProperties tracingProperties,
            IMessageBus eventBus)
        {
            var message = new Message(id: commandId, data: command, tracingProperties);
            var sut = new FlushTableEventsCommandExecutor(Table, TypeResolver, eventBus);

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
            var sut = new FlushTableEventsCommandExecutor(Table, TypeResolver, eventBus);

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
            var eventStore = new TableEventStore<State1>(Table, TypeResolver, brokenEventBus);
            await TryForget(() => eventStore.CollectEvents(streamId, startVersion, events));

            var sut = new FlushTableEventsCommandExecutor(Table, TypeResolver, eventBus);
            var command = new FlushTableEvents(TypeResolver.ResolveTypeName<State1>(), streamId);
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
            var eventStore = new TableEventStore<State1>(Table, TypeResolver, brokenEventBus);
            await TryForget(() => eventStore.CollectEvents(streamId, startVersion, events));

            var sut = new FlushTableEventsCommandExecutor(Table, TypeResolver, eventBus);
            var command = new FlushTableEvents(TypeResolver.ResolveTypeName<State1>(), streamId);
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
