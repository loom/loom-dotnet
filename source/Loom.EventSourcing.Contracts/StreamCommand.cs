namespace Loom.EventSourcing
{
    using System;
    using Loom.Messaging;

    public sealed class StreamCommand<T> : IPartitioned
    {
        public StreamCommand(Guid streamId, T payload)
            => (StreamId, Payload) = (streamId, payload);

        public Guid StreamId { get; }

        public T Payload { get; }

        public string PartitionKey => $"{StreamId}";
    }
}
