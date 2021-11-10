using System;
using System.Threading;
using System.Threading.Tasks;

namespace Loom.Messaging
{
    public interface IDebouncer
    {
        Task Register(
            IDebouncable debouncable,
            CancellationToken cancellationToken = default);

        Task<bool> TryConsume<T>(
            T debouncable,
            Func<T, CancellationToken, Task> consumer,
            CancellationToken cancellationToken = default)
            where T : IDebouncable;
    }
}
