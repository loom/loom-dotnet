namespace Loom.EventSourcing.Azure
{
    using System;

    public sealed class FlushTableEvents
    {
        public FlushTableEvents(string stateType, Guid streamId)
        {
            StateType = stateType;
            StreamId = streamId;
        }

        public string StateType { get; }

        public Guid StreamId { get; }
    }
}
