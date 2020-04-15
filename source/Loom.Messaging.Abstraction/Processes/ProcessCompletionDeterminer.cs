namespace Loom.Messaging.Processes
{
    using System.Collections.Generic;

    public delegate bool ProcessCompletionDeterminer<T>(
        IEnumerable<Message> messages,
        out T result);
}
