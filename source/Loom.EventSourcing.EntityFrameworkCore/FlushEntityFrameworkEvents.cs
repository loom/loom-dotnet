namespace Loom.EventSourcing.EntityFrameworkCore
{
    using System;

    public class FlushEntityFrameworkEvents
    {
        public FlushEntityFrameworkEvents(string stateType, Guid streamId)
        {
            StateType = stateType;
            StreamId = streamId;
        }

        public string StateType { get; }

        public Guid StreamId { get; }
    }
}
