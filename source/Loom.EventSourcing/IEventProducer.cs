namespace Loom.EventSourcing
{
    using System.Collections.Generic;

    public interface IEventProducer<T>
    {
        IEnumerable<object> ProduceEvents(T state, object command);
    }
}
