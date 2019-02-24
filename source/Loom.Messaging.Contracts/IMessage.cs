namespace Loom.Messaging
{
    public interface IMessage
    {
        string Id { get; }
        string OperationId { get; }
        // TODO: Chanage property type to string?.
        string Contributor { get; }
        // TODO: Chanage property type to string?.
        string ParentId { get; }
        dynamic Data { get; }
    }
}
