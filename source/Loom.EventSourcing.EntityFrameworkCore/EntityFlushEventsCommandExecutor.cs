namespace Loom.EventSourcing.EntityFrameworkCore
{
    using System;
    using System.Threading.Tasks;
    using Loom.Json;
    using Loom.Messaging;

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

        public Task Handle(Message message)
            => Execute(command: (FlushEvents)message?.Data);

        private Task Execute(FlushEvents command)
            => _publisher.PublishEvents(command.StateType, command.StreamId);
    }
}
