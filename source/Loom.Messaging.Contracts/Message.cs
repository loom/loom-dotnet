namespace Loom.Messaging
{
    // TODO: Change to positional record.
    public sealed class Message
    {
        // Change the signature to (string id, string processId, string initiator, string? predecessorId).
        public Message(string id, object data, TracingProperties tracingProperties)
        {
            Id = id;
            ProcessId = tracingProperties.OperationId;
            Initiator = tracingProperties.Contributor;
            PredecessorId = tracingProperties.ParentId;
            Data = data;
        }

        public string Id { get; }

        public string ProcessId { get; }

        // TODO: Change the type of the property to string from string?.
        public string? Initiator { get; }

        public string? PredecessorId { get; }

        public object Data { get; }

        // TODO: Remove the property.
        public TracingProperties TracingProperties => new TracingProperties(
            operationId: ProcessId,
            contributor: Initiator,
            parentId: PredecessorId);
    }
}
