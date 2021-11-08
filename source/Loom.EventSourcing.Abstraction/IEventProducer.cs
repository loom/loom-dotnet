using System;
using System.Collections.Generic;

namespace Loom.EventSourcing
{
    [Obsolete("Use interface IEventProducer<TState, TCommand> instead.")]
    public interface IEventProducer<T>
    {
        IEnumerable<object> ProduceEvents(T state, object command);
    }

    public interface IEventProducer<TState, TCommand>
    {
        IEnumerable<object> ProduceEvents(TState state, TCommand command);
    }
}
