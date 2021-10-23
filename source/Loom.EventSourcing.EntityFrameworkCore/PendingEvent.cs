using System;

namespace Loom.EventSourcing.EntityFrameworkCore
{
    public class PendingEvent : IEvent
    {
        private PendingEvent()
        {
        }

        public string StateType { get; private set; }

        public string StreamId { get; private set; }

        public long Version { get; private set; }

        public DateTime RaisedTimeUtc { get; private set; }

        public string EventType { get; private set; }

        public string Payload { get; private set; }

        public string MessageId { get; private set; }

        public string ProcessId { get; private set; }

        public string Initiator { get; private set; }

        public string PredecessorId { get; private set; }

        public Guid Transaction { get; private set; }

        internal static PendingEvent Create(StreamEvent source) => new()
        {
            StateType = source.StateType,
            StreamId = source.StreamId,
            Version = source.Version,
            RaisedTimeUtc = source.RaisedTimeUtc,
            EventType = source.EventType,
            Payload = source.Payload,
            MessageId = source.MessageId,
            ProcessId = source.ProcessId,
            Initiator = source.Initiator,
            PredecessorId = source.PredecessorId,
            Transaction = source.Transaction,
        };
    }
}
