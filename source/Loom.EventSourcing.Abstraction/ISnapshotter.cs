using System.Threading;
using System.Threading.Tasks;

namespace Loom.EventSourcing
{
    public interface ISnapshotter
    {
        Task TakeSnapshot(
            string streamId,
            CancellationToken cancellationToken = default);
    }
}
