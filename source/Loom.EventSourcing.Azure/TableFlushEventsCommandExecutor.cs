using System;
using System.Threading;
using System.Threading.Tasks;
using Loom.Json;
using Loom.Messaging;
using Microsoft.Azure.Cosmos.Table;

namespace Loom.EventSourcing.Azure
{
    public sealed class TableFlushEventsCommandExecutor : IMessageHandler
    {
        private readonly EventPublisher _publisher;

        public TableFlushEventsCommandExecutor(
            CloudTable table,
            TypeResolver typeResolver,
            IJsonProcessor jsonProcessor,
            IMessageBus eventBus)
        {
            _publisher = new EventPublisher(table, typeResolver, jsonProcessor, eventBus);
        }

        public bool CanHandle(Message message)
            => message?.Data is FlushEvents;

        public Task Handle(Message message, CancellationToken cancellationToken = default)
        {
            return message switch
            {
                null => throw new ArgumentNullException(nameof(message)),
                _ => Execute(command: (FlushEvents)message.Data),
            };
        }

        private Task Execute(FlushEvents command)
            => _publisher.PublishEvents(command.StateType, command.StreamId);
    }
}
