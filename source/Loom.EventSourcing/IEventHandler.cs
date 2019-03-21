namespace Loom.EventSourcing
{
    using System.Collections.Generic;

    public interface IEventHandler<T>
    {
        T HandleEvents(T state, IEnumerable<object> events);
    }
}
