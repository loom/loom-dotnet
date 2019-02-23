namespace Loom.EventSourcing
{
    using System.Collections.Generic;

    public interface IEventProducer<T>
    {
        IEnumerable<object> ProduceEventPayloads(
            T state, object commandPayload);
    }
}
