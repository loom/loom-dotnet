using System;

namespace Loom.EventSourcing.EntityFrameworkCore
{
    internal interface IEvent
    {
        string StreamId { get; }

        long Version { get; }

        DateTime RaisedTimeUtc { get; }

        string EventType { get; }

        string Payload { get; }

        string MessageId { get; }

        string ProcessId { get; }

        string Initiator { get; }

        string PredecessorId { get; }
    }
}
