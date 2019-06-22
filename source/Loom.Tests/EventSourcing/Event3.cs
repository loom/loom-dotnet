namespace Loom.EventSourcing
{
    public class Event3
    {
        public Event3(string value) => Value = value;

        public string Value { get; }
    }
}
