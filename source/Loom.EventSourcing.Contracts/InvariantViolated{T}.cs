namespace Loom.EventSourcing
{
    using Loom.Messaging;

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
