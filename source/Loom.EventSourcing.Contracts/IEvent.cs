namespace Loom.EventSourcing
{
    using Loom.Messaging;
    using System;

    public interface IEvent : IVersioned, IPartitioned
    {
        Guid SourceId { get; }

        DateTime RaisedAt { get; }

        dynamic Payload { get; }

        // TODO: Implement IPartitioned.PartitionKey property.
    }
}
