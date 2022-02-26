using System;

namespace Loom.Messaging
{
    public sealed record Message<T>(
        string Id,
        string ProcessId,
        string? Initiator,
        string? PredecessorId,
        T Data)
        where T : notnull
    {
        public static implicit operator Message(Message<T> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return new(source.Id,
                       source.ProcessId,
                       source.Initiator,
                       source.PredecessorId,
                       source.Data);
        }
    }
}
