namespace Loom.EventSourcing
{
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Loom.Testing;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CompositePendingEventScanner_specs
    {
        [TestMethod]
        public void sut_implements_IPendingEventScanner()
        {
            typeof(CompositePendingEventScanner).Should().Implement<IPendingEventScanner>();
        }

        public class Scanner : IPendingEventScanner
        {
            private readonly int _millisecondsDelay;

            public Scanner([Range(1, 1000)] int millisecondsDelay)
            {
                _millisecondsDelay = millisecondsDelay;
            }

            public async Task ScanPendingEvents()
            {
                await Task.Delay(_millisecondsDelay);
                Event.Set();
            }

            public ManualResetEvent Event { get; } = new ManualResetEvent(initialState: default);
        }

        [TestMethod, AutoData]
        public async Task sut_invokes_all_scanners(Scanner[] scanners)
        {
            var sut = new CompositePendingEventScanner(scanners);
            await sut.ScanPendingEvents();
            scanners.All(s => s.Event.WaitOne(millisecondsTimeout: default)).Should().BeTrue();
        }
    }
}
