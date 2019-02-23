namespace Loom.EventSourcing
{
    public interface IVersioned
    {
        int Version { get; }
    }
}
