namespace Loom.EventSourcing
{
    using System;
    using Loom.Messaging;

    public static class InvariantViolated
    {
        public static InvariantViolated<T> Create<T>(
            StreamCommand<T> command,
            ActivityError error)
        {
            return new InvariantViolated<T>(command, error);
        }

        public static InvariantViolated<T> Create<T>(
            StreamCommand<T> command,
            Exception exception)
        {
            if (exception is null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            return new InvariantViolated<T>(
                command,
                new ActivityError(
                    exception.GetType().FullName,
                    exception.Message,
                    exception.StackTrace));
        }
    }
}
