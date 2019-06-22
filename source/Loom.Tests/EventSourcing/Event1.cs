namespace Loom.EventSourcing
{
    public class Event1
    {
        public Event1(int value) => Value = value;

        public int Value { get; }
    }
}
