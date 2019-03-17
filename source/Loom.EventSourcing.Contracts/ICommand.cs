namespace Loom.EventSourcing
{
    using Loom.Messaging;
    using System;

    public interface ICommand : IPartitioned
    {
        Guid StreamId { get; }

        dynamic Payload { get; }

        // TODO: Implement IPartitioned.PartitionKey property.
    }
}
