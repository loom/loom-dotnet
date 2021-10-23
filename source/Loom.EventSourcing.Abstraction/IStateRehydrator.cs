using System.Threading.Tasks;

namespace Loom.EventSourcing
{
    public interface IStateRehydrator<T>
        where T : class
    {
        // TODO: Add a parameter of CancellationToken.
        Task<T?> TryRehydrateState(string streamId);

        // TODO: Add a parameter of CancellationToken.
        Task<T?> TryRehydrateStateAt(string streamId, long version);
    }
}
