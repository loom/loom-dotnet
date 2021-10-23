using System;

namespace Loom.EventSourcing
{
    public static class StreamCommand
    {
        public static StreamCommand<T> Create<T>(Guid streamId, T payload)
            => new(streamId, payload);
    }
}
