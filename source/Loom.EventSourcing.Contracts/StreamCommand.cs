namespace Loom.EventSourcing
{
    public static class StreamCommand
    {
        public static StreamCommand<T> Create<T>(string streamId, T payload)
            => new(streamId, payload);
    }
}
