using System.Collections.Generic;
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
                           IEnumerable<object> events);
    }

    public interface IEventCollector<T> : IEventCollector
    {
    }
}
