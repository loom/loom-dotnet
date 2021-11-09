namespace Loom.EventSourcing
{
    public interface IEventStore : IEventCollector, IEventReader
    {
    }

    public interface IEventStore<T> :
        IEventStore, IEventCollector<T>, IEventReader<T>
    {
    }
}
