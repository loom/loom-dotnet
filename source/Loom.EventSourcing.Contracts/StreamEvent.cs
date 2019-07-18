namespace Loom.EventSourcing
{
    using System;

    public static class StreamEvent
    {
        public static StreamEvent<T> Create<T>(Guid streamId,
                                               long version,
                                               DateTime raisedTimeUtc,
                                               T payload)
        {
            return new StreamEvent<T>(streamId, version, raisedTimeUtc, payload);
        }
    }
}
