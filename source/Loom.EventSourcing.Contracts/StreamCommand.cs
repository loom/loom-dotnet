namespace Loom.EventSourcing
{
    using System;

    public class StreamCommand<T>
    {
        public StreamCommand(Guid streamId, T data)
            => (StreamId, Data) = (streamId, data);

        public Guid StreamId { get; }

        public T Data { get; }
    }
}
