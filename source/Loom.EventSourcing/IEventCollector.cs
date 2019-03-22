namespace Loom.EventSourcing
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IEventCollector
    {
        Task CollectEvents(
            Guid streamId, long firstVersion, IEnumerable<object> events);
    }
}
