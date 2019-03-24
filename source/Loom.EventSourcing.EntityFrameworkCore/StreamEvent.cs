namespace Loom.EventSourcing.EntityFrameworkCore
{
    using System;

    public class StreamEvent
    {
        private StreamEvent()
        {
        }

        public StreamEvent(
            Guid streamId,
            long version,
            DateTime raisedTimeUtc,
            string eventType,
            string eventData)
        {
            StreamId = streamId;
            Version = version;
            RaisedTimeUtc = raisedTimeUtc;
            EventType = eventType;
            EventData = eventData;
        }

        public long Sequence { get; private set; }

        public Guid StreamId { get; private set; }

        public long Version { get; private set; }

        public DateTime RaisedTimeUtc { get; private set; }

        public string EventType { get; private set; }

        public string EventData { get; private set; }
    }
}
