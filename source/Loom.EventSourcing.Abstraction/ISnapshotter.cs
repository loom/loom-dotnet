using System.Threading.Tasks;

namespace Loom.EventSourcing
{
    public interface ISnapshotter
    {
        // TODO: Add a parameter of CancellationToken.
        Task TakeSnapshot(string streamId);
    }
}
