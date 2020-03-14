namespace Loom.EventSourcing.EntityFrameworkCore
{
    using System;

    public class StreamEvent : IEvent
    {
        private StreamEvent()
        {
        }

        internal StreamEvent(string stateType,
                             Guid streamId,
                             long version,
                             DateTime raisedTimeUtc,
                             string eventType,
                             string payload,
                             string messageId,
                             string operationId,
                             string contributor,
                             string parentId,
                             Guid transaction)
        {
            StateType = stateType;
            StreamId = streamId;
            Version = version;
            RaisedTimeUtc = raisedTimeUtc;
            EventType = eventType;
            Payload = payload;
            MessageId = messageId;
            OperationId = operationId;
            Contributor = contributor;
            ParentId = parentId;
            Transaction = transaction;
        }

        public long Sequence { get; private set; }

        public string StateType { get; private set; }

        public Guid StreamId { get; private set; }

        public long Version { get; private set; }

        public DateTime RaisedTimeUtc { get; private set; }

        public string EventType { get; private set; }

        public string Payload { get; private set; }

        public string MessageId { get; private set; }

        public string OperationId { get; private set; }

        public string Contributor { get; private set; }

        public string ParentId { get; private set; }

        public Guid Transaction { get; private set; }
    }
}
