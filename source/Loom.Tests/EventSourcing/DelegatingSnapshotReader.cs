namespace Loom.EventSourcing
{
    using System;
    using System.Threading.Tasks;

    internal class DelegatingSnapshotReader<T> : ISnapshotReader<T>
        where T : class
    {
        private readonly Func<Guid, Task<T>> _function;

        public DelegatingSnapshotReader(Func<Guid, Task<T>> function)
        {
            _function = function;
        }

        public Task<T> TryRestoreSnapshot(Guid streamId)
        {
            return _function.Invoke(streamId);
        }
    }
}
