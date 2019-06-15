namespace Loom.EventSourcing
{
    using System;

    public class StreamEvent<T>
    {
        public StreamEvent(Guid streamId, long version, T payload)
            => (StreamId, Version, Payload) = (streamId, version, payload);

        public Guid StreamId { get; }

        public long Version { get; }

        public T Payload { get; }
    }
}
