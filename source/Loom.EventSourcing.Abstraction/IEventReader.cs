using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Loom.EventSourcing
{
    public interface IEventReader
    {
        Task<IEnumerable<object>> QueryEvents(
            string streamId,
            long fromVersion,
            CancellationToken cancellationToken = default);
    }

    public interface IEventReader<T> : IEventReader
    {
    }
}
