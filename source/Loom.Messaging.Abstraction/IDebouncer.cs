namespace Loom.Messaging
{
    using System;
    using System.Threading.Tasks;

    public interface IDebouncer
    {
        Task Register(IDebouncable debouncable);

        Task<bool> TryProceed<T>(T debouncable, Func<T, Task> proceed)
            where T : IDebouncable;
    }
}
