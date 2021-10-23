using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Loom.Messaging;

namespace Loom.EventSourcing.InMemory
{
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
