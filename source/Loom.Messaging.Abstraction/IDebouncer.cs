namespace Loom.Messaging
{
    using System;
    using System.Threading.Tasks;

    public interface IDebouncer
    {
        Task Register(IDebouncable debouncable);

        Task<bool> TryConsume<T>(T debouncable, Func<T, Task> consumer)
            where T : IDebouncable;
    }
}
