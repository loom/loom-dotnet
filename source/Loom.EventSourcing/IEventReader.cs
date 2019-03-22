namespace Loom.EventSourcing
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IEventReader
    {
        Task<IEnumerable<object>> QueryEvents(Guid streamId, long fromVersion);
    }

    // TODO: Implement Task<IEnumerable<object>> QueryEvents(Guid streamId) method.
}
