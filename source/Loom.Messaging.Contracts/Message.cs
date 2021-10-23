namespace Loom.Messaging
{
    // TODO: Change to positional record.
    public sealed class Message
    {
        public Message(
            string id,
            string processId,
            string? initiator,
            string? predecessorId,
            object data)
        {
            Id = id;
            ProcessId = processId;
            Initiator = initiator;
            PredecessorId = predecessorId;
            Data = data;
        }

        public string Id { get; }

        public string ProcessId { get; }

        // TODO: Change the type of the property to string from string?.
        public string? Initiator { get; }

        public string? PredecessorId { get; }

        public object Data { get; }
    }
}
