using System;

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

        // TODO: Remove the property.
        [Obsolete("Use metadata properties directly instead.")]
        public TracingProperties TracingProperties => new(
            operationId: ProcessId,
            contributor: Initiator,
            parentId: PredecessorId);

        [Obsolete("Use the public constructor instead")]
        public static Message Create(
            string id,
            object data,
            TracingProperties tracingProperties)
        {
            return new Message(
                id,
                processId: tracingProperties.OperationId,
                initiator: tracingProperties.Contributor,
                predecessorId: tracingProperties.ParentId,
                data);
        }
    }
}
