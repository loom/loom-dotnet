namespace Loom.Messaging.Processes
{
    using System;
    using System.Runtime.CompilerServices;

    public sealed class ProcessOptions
    {
        public const int MaxTimeoutInSeconds = 300;

        private static readonly TimeSpan _maxTimeout = TimeSpan.FromSeconds(MaxTimeoutInSeconds);

        public ProcessOptions(
            Func<object, bool> completionDeterminer,
            TimeSpan timeout)
        {
            TimeoutGuard(timeout);

            CompletionDeterminer = completionDeterminer;
            Timeout = timeout;
        }

        public Func<object, bool> CompletionDeterminer { get; }

        public TimeSpan Timeout { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void TimeoutGuard(TimeSpan timeout)
        {
            if (timeout > _maxTimeout)
            {
                string message = $"The parameter '{nameof(timeout)}' must be less than 5 minutes.";
                throw new ArgumentOutOfRangeException(paramName: nameof(timeout), message);
            }
        }
    }
}
