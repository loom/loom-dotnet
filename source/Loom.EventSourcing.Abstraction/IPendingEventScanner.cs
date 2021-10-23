using System.Threading.Tasks;

namespace Loom.EventSourcing
{
    public interface IPendingEventScanner
    {
        // TODO: Add a parameter of CancellationToken.
        Task ScanPendingEvents();
    }
}
