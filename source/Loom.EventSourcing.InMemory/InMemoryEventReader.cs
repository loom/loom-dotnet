namespace Loom.EventSourcing.InMemory
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Loom.Messaging;

    public class InMemoryEventReader<T> : IEventReader<T>
    {
        private readonly InMemoryEventSourcingEngine<T> _engine;

        public InMemoryEventReader(InMemoryEventSourcingEngine<T> engine)
        {
            _engine = engine;
        }

        public InMemoryEventReader()
            : this(InMemoryEventSourcingEngine<T>.Default)
        {
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
