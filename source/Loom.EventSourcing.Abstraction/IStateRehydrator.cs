using System.Threading.Tasks;

namespace Loom.EventSourcing
{
    public interface IStateRehydrator<T>
        where T : class
    {
        Task<T?> TryRehydrateState(string streamId);

        Task<T?> TryRehydrateStateAt(string streamId, long version);
    }
}
