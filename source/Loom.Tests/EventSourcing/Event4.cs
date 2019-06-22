namespace Loom.EventSourcing
{
    using System;

    public class Event4
    {
        public Event4(Guid value) => Value = value;

        public Guid Value { get; }
    }
}
