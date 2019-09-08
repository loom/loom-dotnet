namespace Loom.Messaging
{
    public interface IDebouncable
    {
        string Source { get; }

        string Context { get; }
    }
}
