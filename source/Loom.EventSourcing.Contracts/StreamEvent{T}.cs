using System;

namespace Loom.EventSourcing
{
    public sealed class StreamEvent<T>
    {
        public StreamEvent(string streamId,
                           long version,
                           DateTime raisedTimeUtc,
                           T payload)
        {
            StreamId = streamId;
            Version = version;
            RaisedTimeUtc = raisedTimeUtc;
            Payload = payload;
        }

        public string StreamId { get; }

        public long Version { get; }

        public DateTime RaisedTimeUtc { get; }

        public T Payload { get; }

        internal void Deconstruct(out string streamId,
                                  out long version,
                                  out DateTime raisedTimeUtc,
                                  out T payload)
        {
            streamId = StreamId;
            version = Version;
            raisedTimeUtc = RaisedTimeUtc;
            payload = Payload;
        }
    }
}
