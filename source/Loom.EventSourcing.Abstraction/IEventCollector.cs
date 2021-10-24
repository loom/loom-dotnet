using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Loom.EventSourcing
{
    public interface IEventCollector
    {
        Task CollectEvents(string processId,
                           string? initiator,
                           string? predecessorId,
                           string streamId,
                           long startVersion,
                           IEnumerable<object> events,
                           CancellationToken cancellationToken);
    }

    public interface IEventCollector<T> : IEventCollector
    {
    }
}
