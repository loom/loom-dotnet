namespace Loom.EventSourcing
{
    using System;
    using System.Threading.Tasks;

    public interface IStateRehydrator<T>
        where T : class
    {
        Task<T?> TryRehydrateState(Guid streamId);

        Task<T?> TryRehydrateStateAt(Guid streamId, long version);
    }
}
