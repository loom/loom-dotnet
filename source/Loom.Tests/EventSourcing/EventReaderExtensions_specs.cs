using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Loom.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Loom.EventSourcing
{
    [TestClass]
    public class EventReaderExtensions_specs
    {
        [TestMethod, AutoData]
        public async Task QueryEvents_relays_correctly(
            IEventReader reader, string streamId, IEnumerable<object> events)
        {
            long fromVersion = 1;
            Mock.Get(reader).Setup(x => x.QueryEvents(streamId, fromVersion)).ReturnsAsync(events);

            IEnumerable<object> actual = await reader.QueryEvents(streamId);

            actual.Should().BeSameAs(events);
        }
    }
}
