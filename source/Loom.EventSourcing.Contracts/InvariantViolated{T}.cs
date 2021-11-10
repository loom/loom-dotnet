using Loom.Messaging;

namespace Loom.EventSourcing
{
    public sealed class InvariantViolated<T>
    {
        public InvariantViolated(StreamCommand<T> command, ActivityError error)
        {
            Command = command;
            Error = error;
        }

        public StreamCommand<T> Command { get; }

        public ActivityError Error { get; }
    }
}
