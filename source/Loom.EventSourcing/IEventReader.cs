namespace Loom.EventSourcing
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IEventReader
    {
        Task<IEnumerable<object>> QueryEventPayloads(
            Guid sourceId, int afterVersion);
    }

    // TODO: Implement Task<IEnumerable<object>> QueryEventPayloads(Guid sourceId) method.
}
