using System;
using System.Threading.Tasks;

namespace Loom.EventSourcing
{
    [Obsolete("This class will be replaced with new framework.")]
    public interface IStateRehydrator<T>
        where T : class
    {
        Task<T?> TryRehydrateState(string streamId);

        Task<T?> TryRehydrateStateAt(string streamId, long version);
    }
}
