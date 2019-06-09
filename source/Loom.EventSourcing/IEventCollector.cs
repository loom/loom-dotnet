namespace Loom.EventSourcing
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IEventCollector
    {
        Task CollectEvents(string operationId,
                           string contributor,
                           string parentId,
                           Guid streamId,
                           long startVersion,
                           IEnumerable<object> events);
    }
}
