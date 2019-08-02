namespace Loom.EventSourcing
{
    using System;
    using Loom.Messaging;

    public sealed class StreamCommandFailed<T>
    {
        // TODO: Remove guard clauses after apply C# 8.0.
        public StreamCommandFailed(StreamCommand<T> command,
                                   HandlerError error,
                                   DateTime failedTimeUtc)
        {
            Command = command ?? throw new ArgumentNullException(nameof(command));
            Error = error ?? throw new ArgumentNullException(nameof(error));
            FailedTimeUtc = failedTimeUtc;
        }

        public StreamCommand<T> Command { get; }

        public HandlerError Error { get; }

        public DateTime FailedTimeUtc { get; }
    }
}
