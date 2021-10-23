namespace Loom.Messaging
{
    public sealed record Message(
        string Id,
        string ProcessId,
        string? Initiator,
        string? PredecessorId,
        object Data);
}
