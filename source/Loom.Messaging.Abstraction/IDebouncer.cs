using System;
using System.Threading.Tasks;

namespace Loom.Messaging
{
    public interface IDebouncer
    {
        // TODO: Add a parameter of CancellationToken.
        Task Register(IDebouncable debouncable);

        // TODO: Add a parameter of CancellationToken.
        Task<bool> TryConsume<T>(T debouncable, Func<T, Task> consumer)
            where T : IDebouncable;
    }
}
