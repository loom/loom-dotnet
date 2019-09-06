namespace Loom.EventSourcing
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    // TODO: Implement Task<IEnumerable<object>> QueryEvents(Guid streamId) method.
    public interface IEventReader
    {
        Task<IEnumerable<object>> QueryEvents(Guid streamId, long fromVersion);
    }

    public interface IEventReader<T> : IEventReader
    {
    }
}
