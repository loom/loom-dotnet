namespace Loom.EventSourcing
{
    using System;
    using Loom.Messaging;

    public static class InvariantViolated
    {
        public static InvariantViolated<T> Create<T>(
            Guid streamId,
            T command,
            ActivityError error)
        {
            return new InvariantViolated<T>(streamId, command, error);
        }
    }
}
