namespace Loom.EventSourcing
{
    using System;
    using System.Threading.Tasks;

    public interface ISnapshotReader<T>
        where T : class
    {
        Task<T?> TryRestoreSnapshot(Guid streamId);
    }
}
