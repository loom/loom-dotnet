using System.Threading;
using System.Threading.Tasks;

namespace Loom.EventSourcing
{
    public interface ISnapshotReader<T>
        where T : class
    {
        Task<T?> TryRestoreSnapshot(
            string streamId,
            CancellationToken cancellationToken = default);
    }
}
