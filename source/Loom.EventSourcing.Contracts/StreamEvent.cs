namespace Loom.EventSourcing
{
    using System;
    using Loom.Messaging;

    public sealed class StreamEvent<T> : IPartitioned
    {
        public StreamEvent(Guid streamId, long version, DateTime raisedTimeUtc, T payload)
        {
            StreamId = streamId;
            Version = version;
            RaisedTimeUtc = raisedTimeUtc;
            Payload = payload;
        }

        public Guid StreamId { get; }

        public long Version { get; }

        public DateTime RaisedTimeUtc { get; }

        public T Payload { get; }

        public string PartitionKey => $"{StreamId}";
    }
}
