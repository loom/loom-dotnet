using System;
using System.Collections.Generic;

namespace Loom.EventSourcing
{
    [Obsolete("Use interface IEventHandler<TState, TEvent> instead.")]
    public interface IEventHandler<T>
    {
        T HandleEvents(T state, IEnumerable<object> events);
    }

    public interface IEventHandler<TState, TEvent>
    {
        TState HandleEvent(TState state, TEvent pastEvent);
    }
}
