namespace Loom.EventSourcing
{
    public class Event2
    {
        public Event2(double value) => Value = value;

        public double Value { get; }
    }
}
