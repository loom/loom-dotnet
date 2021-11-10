using System.Collections.Generic;

namespace Loom.EventSourcing
{
    public interface IEventProducer<TState, TCommand>
    {
        IEnumerable<object> ProduceEvents(TState state, TCommand command);
    }
}
