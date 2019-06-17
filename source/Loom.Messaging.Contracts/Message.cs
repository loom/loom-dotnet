namespace Loom.Messaging
{
    public sealed class Message
    {
        public Message(string id, object data, TracingProperties tracingProperties)
        {
            Id = id;
            Data = data;
            TracingProperties = tracingProperties;
        }

        public string Id { get; }

        public object Data { get; }

        public TracingProperties TracingProperties { get; }
    }
}
