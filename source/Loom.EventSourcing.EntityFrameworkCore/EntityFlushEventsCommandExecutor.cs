using System;
using System.Threading;
using System.Threading.Tasks;
using Loom.Json;
using Loom.Messaging;

namespace Loom.EventSourcing.EntityFrameworkCore
{
    public sealed class EntityFlushEventsCommandExecutor : IMessageHandler
    {
        private readonly EventPublisher _publisher;

        public EntityFlushEventsCommandExecutor(
            Func<EventStoreContext> contextFactory,
            TypeResolver typeResolver,
            IJsonProcessor jsonProcessor,
            IMessageBus eventBus)
        {
            _publisher = new EventPublisher(contextFactory, typeResolver, jsonProcessor, eventBus);
        }

        public bool CanHandle(Message message)
            => message?.Data is FlushEvents;

        public Task Handle(Message message, CancellationToken cancellationToken = default)
            => Execute(command: (FlushEvents)message?.Data, cancellationToken);

        private Task Execute(FlushEvents command, CancellationToken cancellationToken)
            => _publisher.PublishEvents(command.StateType, command.StreamId, cancellationToken);
    }
}
