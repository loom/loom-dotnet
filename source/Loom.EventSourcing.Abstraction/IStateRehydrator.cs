namespace Loom.EventSourcing
{
    using System;
    using System.Threading.Tasks;

    public interface IStateRehydrator<T>
    {
        // TODO: Change return type to Task<T?>.
        Task<T> TryRehydrateState(Guid streamId);

        // TODO: Change return type to Task<T?>.
        Task<T> TryRehydrateStateAt(Guid streamId, long version);

        // TODO: Implement Task<T> RehydrateState(Guid streamId) method.
    }
}
