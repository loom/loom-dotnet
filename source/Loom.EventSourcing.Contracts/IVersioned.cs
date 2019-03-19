namespace Loom.EventSourcing
{
    public interface IVersioned
    {
        long Version { get; }
    }
}
