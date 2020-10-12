namespace Loom.Messaging
{
    public sealed class Message<T>
    {
        public Message(string id, T data, TracingProperties tracingProperties)
        {
            Id = id;
            Data = data;
            TracingProperties = tracingProperties;
        }

        public string Id { get; }

        public T Data { get; }

        public TracingProperties TracingProperties { get; }
    }
}
