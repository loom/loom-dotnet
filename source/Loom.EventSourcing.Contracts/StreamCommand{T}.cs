namespace Loom.EventSourcing
{
    public sealed class StreamCommand<T>
    {
        public StreamCommand(string streamId, T payload)
            => (StreamId, Payload) = (streamId, payload);

        public string StreamId { get; }

        public T Payload { get; }
    }
}
