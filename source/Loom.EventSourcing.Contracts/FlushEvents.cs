namespace Loom.EventSourcing
{
    public sealed record FlushEvents(string StateType, string StreamId);
}
