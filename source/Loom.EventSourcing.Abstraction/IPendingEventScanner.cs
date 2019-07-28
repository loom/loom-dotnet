namespace Loom.EventSourcing
{
    using System.Threading.Tasks;

    public interface IPendingEventScanner
    {
        Task ScanPendingEvents();
    }
}
