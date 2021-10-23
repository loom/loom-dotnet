using System.Threading.Tasks;

namespace Loom.EventSourcing
{
    public interface ISnapshotReader<T>
        where T : class
    {
        // TODO: Add a parameter of CancellationToken.
        Task<T?> TryRestoreSnapshot(string streamId);
    }
}
