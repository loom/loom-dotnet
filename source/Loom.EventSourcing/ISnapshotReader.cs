namespace Loom.EventSourcing
{
    using System;
    using System.Threading.Tasks;

    public interface ISnapshotReader<T>
    {
        // TODO: Change return type to Task<T?>.
        Task<T> TryRestoreSnapshot(Guid streamId);
    }
}
