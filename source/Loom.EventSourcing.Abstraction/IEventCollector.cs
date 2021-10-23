using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Loom.Messaging;

namespace Loom.EventSourcing
{
    public interface IEventCollector
    {
        Task CollectEvents(string processId,
                           string? initiator,
                           string? predecessorId,
                           string streamId,
                           long startVersion,
                           IEnumerable<object> events);

        [Obsolete("Use metadata decapsulated overload instead.")]
        Task CollectEvents(string streamId,
                           long startVersion,
                           IEnumerable<object> events,
                           TracingProperties tracingProperties = default);
    }

    public interface IEventCollector<T> : IEventCollector
    {
    }
}
