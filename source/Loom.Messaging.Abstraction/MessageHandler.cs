namespace Loom.Messaging
{
    using System.Threading.Tasks;

    public abstract class MessageHandler<T> : IMessageHandler
    {
        public bool CanHandle(Message message) => message?.Data is T;

        public Task Handle(Message message)
            => HandleMessage(message?.Id, (T)message?.Data, message?.TracingProperties ?? default);

        public abstract Task HandleMessage(string id, T data, TracingProperties tracingProperties);
    }
}
