namespace Loom.EventSourcing.EntityFrameworkCore
{
    using System;

    internal interface IEvent
    {
        Guid StreamId { get; }

        long Version { get; }

        DateTime RaisedTimeUtc { get; }

        string EventType { get; }

        string Payload { get; }

        string MessageId { get; }

        string OperationId { get; }

        string Contributor { get; }

        string ParentId { get; }
    }
}
