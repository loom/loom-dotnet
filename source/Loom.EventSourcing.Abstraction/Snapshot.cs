namespace Loom.EventSourcing
{
    public sealed record Snapshot<T>(int Version, T State);
}
