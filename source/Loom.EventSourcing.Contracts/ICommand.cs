namespace Loom.EventSourcing
{
    using Loom.Messaging;
    using System;

    public interface ICommand : IPartitioned
    {
        Guid TargetId { get; }
        dynamic Payload { get; }

        // TODO: Implement IPartitioned.PartitionKey property.
    }
}
