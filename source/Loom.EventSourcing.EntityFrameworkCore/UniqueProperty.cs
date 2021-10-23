namespace Loom.EventSourcing.EntityFrameworkCore
{
    public class UniqueProperty
    {
        private UniqueProperty()
        {
        }

        public UniqueProperty(string stateType, string streamId, string name, string value)
        {
            StateType = stateType;
            StreamId = streamId;
            Name = name;
            Value = value;
        }

        public long Sequence { get; private set; }

        public string StateType { get; private set; }

        public string StreamId { get; private set; }

        public string Name { get; private set; }

        public string Value { get; private set; }

        public void SetValue(string value) => Value = value;
    }
}
