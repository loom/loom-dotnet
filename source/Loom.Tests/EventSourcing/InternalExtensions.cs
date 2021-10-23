using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;

namespace Loom.EventSourcing
{
    internal static class InternalExtensions
    {
        private static readonly Fixture _builder = new();

        public static Task CollectEvents(
            this IEventCollector collector,
            string streamId,
            long startVersion,
            IEnumerable<object> events)
        {
            return collector.CollectEvents(
                processId: _builder.Create<string>(),
                initiator: _builder.Create<string>(),
                predecessorId: _builder.Create<string>(),
                streamId,
                startVersion,
                events);
        }
    }
}
