namespace Loom.EventSourcing
{
    public sealed record Snapshot<T>(string StreamId, long Version, T State) : IVersioned;
}
