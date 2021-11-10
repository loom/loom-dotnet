namespace Loom.EventSourcing
{
    public interface IEventHandler<TState, TEvent>
    {
        TState HandleEvent(TState state, TEvent pastEvent);
    }
}
