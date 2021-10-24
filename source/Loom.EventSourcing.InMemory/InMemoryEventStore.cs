using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Loom.Messaging;

namespace Loom.EventSourcing.InMemory
{
    public class InMemoryEventStore<T> :
        IEventStore<T>,
        IEventCollector,
        IEventReader
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

        public Task CollectEvents(string processId,
                                  string initiator,
                                  string predecessorId,
                                  string streamId,
                                  long startVersion,
                                  IEnumerable<object> events,
                                  CancellationToken cancellationToken)
        {
            if (events is null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            IEnumerable<Message> messages = _engine.CollectEvents(
                processId,
                initiator,
                predecessorId,
                streamId,
                startVersion,
                events);

            return _eventBus.Send(messages, partitionKey: $"{streamId}");
        }

        public Task<IEnumerable<object>> QueryEvents(
            string streamId,
            long fromVersion)
        {
            return Task.FromResult(_engine.QueryEvents(streamId, fromVersion));
        }

        public Task<IEnumerable<Message>> QueryEventMessages(
            string streamId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_engine.QueryEventMessages(streamId));
        }
    }
}
