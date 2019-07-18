namespace Loom.EventSourcing
{
    using System;

    public static class StreamCommand
    {
        public static StreamCommand<T> Create<T>(Guid streamId, T payload)
            => new StreamCommand<T>(streamId, payload);
    }
}
