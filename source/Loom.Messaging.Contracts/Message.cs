namespace Loom.Messaging
{
    public sealed class Message
    {
        // TODO: Change the type of parameter 'operationId' to string?.
        // TODO: Change the type of parameter 'contributor' to string?.
        // TODO: Change the type of parameter 'parentId' to string?.
        public Message(string id,
                       string operationId,
                       string contributor,
                       string parentId,
                       object data)
        {
            Id = id;
            OperationId = operationId;
            Contributor = contributor;
            ParentId = parentId;
            Data = data;
        }

        public string Id { get; }

        // TODO: Change the type to string?.
        public string OperationId { get; }

        // TODO: Change the type to string?.
        public string Contributor { get; }

        // TODO: Change the type to string?.
        public string ParentId { get; }

        public object Data { get; }
    }
}
