namespace Loom.EventSourcing
{
    public sealed class FlushEvents
    {
        public FlushEvents(string stateType, string streamId)
        {
            StateType = stateType;
            StreamId = streamId;
        }

        public string StateType { get; }

        public string StreamId { get; }
    }
}
