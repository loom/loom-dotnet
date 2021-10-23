using System;

namespace Loom.EventSourcing.EntityFrameworkCore
{
    public class StreamEvent : IEvent
    {
        private StreamEvent()
        {
        }

        internal StreamEvent(string stateType,
                             string messageId,
                             string processId,
                             string initiator,
                             string predecessorId,
                             string streamId,
                             long version,
                             DateTime raisedTimeUtc,
                             string eventType,
                             string payload,
                             Guid transaction)
        {
            StateType = stateType;
            StreamId = streamId;
            Version = version;
            RaisedTimeUtc = raisedTimeUtc;
            EventType = eventType;
            Payload = payload;
            MessageId = messageId;
            ProcessId = processId;
            Initiator = initiator;
            PredecessorId = predecessorId;
            Transaction = transaction;
        }

        public long Sequence { get; private set; }

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
    }
}
