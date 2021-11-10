namespace Loom.EventSourcing
{
    public sealed record Snapshot<T>(long Version, T State) : IVersioned;
}
