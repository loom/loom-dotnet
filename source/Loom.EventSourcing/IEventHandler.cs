namespace Loom.EventSourcing
{
    public interface IEventHandler<T>
    {
        T Handle(T state, object eventPayload);
    }

    // TODO: Implement T HandleRange(T state, IEnumerable<object> eventPayloads) method.
}
