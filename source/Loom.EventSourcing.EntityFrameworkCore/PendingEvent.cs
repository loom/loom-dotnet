namespace Loom.EventSourcing.EntityFrameworkCore
{
    using System;
    using Loom.Messaging;

    public class PendingEvent
    {
        private PendingEvent()
        {
        }

        internal PendingEvent(StreamEvent source)
        {
            StateType = source.StateType;
            StreamId = source.StreamId;
            Version = source.Version;
            RaisedTimeUtc = source.RaisedTimeUtc;
            EventType = source.EventType;
            Payload = source.Payload;
            MessageId = source.MessageId;
            OperationId = source.OperationId;
            Contributor = source.Contributor;
            ParentId = source.ParentId;
            Transaction = source.Transaction;
        }

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

        internal TracingProperties TracingProperties => new TracingProperties(OperationId, Contributor, ParentId);
    }
}
