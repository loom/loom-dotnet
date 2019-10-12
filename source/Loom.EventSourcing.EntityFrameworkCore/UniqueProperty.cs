namespace Loom.EventSourcing.EntityFrameworkCore
{
    using System;

    public class UniqueProperty
    {
        private UniqueProperty()
        {
        }

        public UniqueProperty(string stateType, Guid streamId, string name, string value)
        {
            StateType = stateType;
            StreamId = streamId;
            Name = name;
            Value = value;
        }

        public long Sequence { get; private set; }

        public string StateType { get; private set; }

        public Guid StreamId { get; private set; }

        public string Name { get; private set; }

        public string Value { get; private set; }

        public void SetValue(string value) => Value = value;
    }
}
