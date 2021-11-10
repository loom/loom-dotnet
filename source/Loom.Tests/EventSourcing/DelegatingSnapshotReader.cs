using System;
using System.Threading;
using System.Threading.Tasks;

namespace Loom.EventSourcing
{
    internal class DelegatingSnapshotReader<T> : ISnapshotReader<T>
        where T : class
    {
        private readonly Func<string, Task<T>> _function;

        public DelegatingSnapshotReader(Func<string, Task<T>> function)
        {
            _function = function;
        }

        public Task<T> TryRestoreSnapshot(
            string streamId,
            CancellationToken cancellationToken = default)
        {
            return _function.Invoke(streamId);
        }
    }
}
