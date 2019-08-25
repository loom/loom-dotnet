namespace Loom.EventSourcing.Azure
{
    using System.Threading.Tasks;
    using Loom.Json;
    using Loom.Messaging;
    using Microsoft.Azure.Cosmos.Table;

    public sealed class FlushTableEventsCommandExecutor : IMessageHandler
    {
        private readonly EventPublisher _publisher;

        public FlushTableEventsCommandExecutor(
            CloudTable table,
            TypeResolver typeResolver,
            IJsonProcessor jsonProcessor,
            IMessageBus eventBus)
        {
            _publisher = new EventPublisher(table, typeResolver, jsonProcessor, eventBus);
        }

        public bool CanHandle(Message message)
            => message?.Data is FlushTableEvents;

        public Task Handle(Message message)
            => Execute(command: (FlushTableEvents)message?.Data);

        private Task Execute(FlushTableEvents command)
            => _publisher.PublishEvents(command.StateType, command.StreamId);
    }
}
