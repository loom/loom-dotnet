namespace Loom.EventSourcing
{
    using System;

    public interface IEntity
    {
        Guid Id { get; }
    }
}
