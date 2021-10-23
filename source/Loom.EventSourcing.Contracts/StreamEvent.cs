using System;

namespace Loom.EventSourcing
{
    public static class StreamEvent
    {
        public static StreamEvent<T> Create<T>(
            string streamId,
            long version,
            DateTime raisedTimeUtc,
            T payload)
        {
            return new(streamId, version, raisedTimeUtc, payload);
        }
    }
}
