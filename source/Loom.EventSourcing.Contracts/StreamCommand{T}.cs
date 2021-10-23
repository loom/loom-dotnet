namespace Loom.EventSourcing
{
    public sealed record StreamCommand<T>(string StreamId, T Payload);
}
