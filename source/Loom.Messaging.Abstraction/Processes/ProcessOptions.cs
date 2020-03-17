namespace Loom.Messaging.Processes
{
    using System;

    public sealed class ProcessOptions
    {
        public ProcessOptions(
            Func<object, bool> completionDeterminer,
            TimeSpan timeout)
        {
            CompletionDeterminer = completionDeterminer;
            Timeout = timeout;
        }

        public Func<object, bool> CompletionDeterminer { get; }

        public TimeSpan Timeout { get; }
    }
}
