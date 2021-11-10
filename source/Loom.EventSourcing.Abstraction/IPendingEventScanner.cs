using System;
using System.Threading.Tasks;

namespace Loom.EventSourcing
{
    [Obsolete("This class will be replaced with new framework.")]
    public interface IPendingEventScanner
    {
        Task ScanPendingEvents();
    }
}
