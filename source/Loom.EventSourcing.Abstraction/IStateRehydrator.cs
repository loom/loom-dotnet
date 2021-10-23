using System;
using System.Threading.Tasks;

namespace Loom.EventSourcing
{
    public interface IStateRehydrator<T>
        where T : class
    {
        Task<T?> TryRehydrateState(Guid streamId);

        Task<T?> TryRehydrateStateAt(Guid streamId, long version);
    }
}
