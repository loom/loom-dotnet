namespace Loom.EventSourcing.InMemory
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Loom.Messaging;

    public class InMemoryEventReader<T> : IEventReader<T>
    {
        public Task<IEnumerable<object>> QueryEvents(
            Guid streamId,
            long fromVersion)
        {
            return Task.FromResult(InMemoryEventSourcingEngine<T>.QueryEvents(streamId, fromVersion));
        }

        public Task<IEnumerable<Message>> QueryEventMessages(
            Guid streamId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(InMemoryEventSourcingEngine<T>.QueryEventMessages(streamId));
        }
    }
}
