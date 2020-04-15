namespace Loom.Messaging.Processes
{
    using System;
    using System.Collections.Generic;
    using Loom.Messaging;

    public sealed class ProcessCompletionStrategy<T>
    {
        public ProcessCompletionStrategy(
            ProcessCompletionDeterminer<T> completionDeterminer,
            TimeSpan timeout,
            Func<IEnumerable<Message>, T> timeoutHandler)
        {
            CompletionDeterminer = completionDeterminer;
            Timeout = timeout;
            TimeoutHandler = timeoutHandler;
        }

        public ProcessCompletionDeterminer<T> CompletionDeterminer { get; }

        public TimeSpan Timeout { get; }

        public Func<IEnumerable<Message>, T> TimeoutHandler { get; }
    }
}
