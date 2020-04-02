namespace Loom.EventSourcing
{
    using System;
    using Loom.Messaging;

    public sealed class InvariantViolated<T>
    {
        public InvariantViolated(Guid streamId, T command, ActivityError error)
        {
            StreamId = streamId;
            Command = command;
            Error = error;
        }

        public Guid StreamId { get; }

        public T Command { get; }

        public ActivityError Error { get; }
    }
}
