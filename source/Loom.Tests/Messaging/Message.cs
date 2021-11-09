namespace Loom.Messaging
{
    public sealed record Message<T>(
        string Id,
        string ProcessId,
        string Initiator,
        string PredecessorId,
        T data)
    {
        public static implicit operator Message(Message<T> source)
        {
            return new(source.Id,
                       source.ProcessId,
                       source.Initiator,
                       source.PredecessorId,
                       source.data);
        }
    }
}
