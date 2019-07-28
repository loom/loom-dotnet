namespace Loom.EventSourcing.EntityFrameworkCore
{
    using System;
    using System.Threading.Tasks;
    using Loom.Messaging;

    public sealed class FlushEntityFrameworkEventsCommandExecutor : IMessageHandler
    {
        private readonly EventPublisher _publisher;

        public FlushEntityFrameworkEventsCommandExecutor(
            Func<EventStoreContext> contextFactory,
            TypeResolver typeResolver,
            IMessageBus eventBus)
        {
            _publisher = new EventPublisher(contextFactory, typeResolver, eventBus);
        }

        public bool CanHandle(Message message)
            => message?.Data is FlushEntityEvents;

        public Task Handle(Message message)
            => Execute(command: (FlushEntityEvents)message?.Data);

        private Task Execute(FlushEntityEvents command)
            => _publisher.PublishEvents(command.StateType, command.StreamId);
    }
}
