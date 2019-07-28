namespace Loom.EventSourcing
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class CompositePendingEventScanner : IPendingEventScanner
    {
        private readonly IEnumerable<IPendingEventScanner> _scanners;

        public CompositePendingEventScanner(params IPendingEventScanner[] scanners)
        {
            _scanners = scanners.ToList().AsReadOnly();
        }

        public Task ScanPendingEvents()
            => Task.WhenAll(_scanners.Select(s => s.ScanPendingEvents()));
    }
}
