namespace Loom.EventSourcing.InMemory
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Loom.Messaging;

    public class InMemoryEventStore<T> : IEventStore<T>
    {
        private readonly InMemoryEventSourcingEngine<T> _engine;
        private readonly IMessageBus _eventBus;

        public InMemoryEventStore(
            InMemoryEventSourcingEngine<T> engine, IMessageBus eventBus)
        {
            _engine = engine;
            _eventBus = eventBus;
        }

        public InMemoryEventStore(IMessageBus eventBus)
            : this(InMemoryEventSourcingEngine<T>.Default, eventBus)
        {
        }

        public Task CollectEvents(
            Guid streamId,
            long startVersion,
            IEnumerable<object> events,
            TracingProperties tracingProperties = default)
        {
            if (events is null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            IEnumerable<Message> messages = _engine.CollectEvents(
                streamId,
                startVersion,
                events,
                tracingProperties);

            return _eventBus.Send(messages, partitionKey: $"{streamId}");
        }

        public Task<IEnumerable<object>> QueryEvents(
            Guid streamId,
            long fromVersion)
        {
            return Task.FromResult(_engine.QueryEvents(streamId, fromVersion));
        }

        public Task<IEnumerable<Message>> QueryEventMessages(
            Guid streamId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_engine.QueryEventMessages(streamId));
        }
    }
}
