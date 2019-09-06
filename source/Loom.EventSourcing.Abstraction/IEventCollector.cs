namespace Loom.EventSourcing
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Loom.Messaging;

    public interface IEventCollector
    {
        Task CollectEvents(Guid streamId,
                           long startVersion,
                           IEnumerable<object> events,
                           TracingProperties tracingProperties = default);
    }

    public interface IEventCollector<T> : IEventCollector
    {
    }
}
