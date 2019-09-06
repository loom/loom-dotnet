namespace Loom.EventSourcing
{
    public interface IEventStore<T> :
        IEventCollector<T>, IEventCollector, IEventReader<T>, IEventReader
    {
    }
}
