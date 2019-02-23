namespace Loom.Messaging
{
    public interface IPartitioned
    {
        string PartitionKey { get; }
    }
}
