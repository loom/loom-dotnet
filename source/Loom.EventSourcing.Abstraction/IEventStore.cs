namespace Loom.EventSourcing
{
    public interface IEventStore<T> : IEventCollector, IEventReader
    {
    }
}
