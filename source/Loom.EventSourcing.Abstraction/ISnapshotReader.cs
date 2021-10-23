using System;
using System.Threading.Tasks;

namespace Loom.EventSourcing
{
    public interface ISnapshotReader<T>
        where T : class
    {
        Task<T?> TryRestoreSnapshot(Guid streamId);
    }
}
