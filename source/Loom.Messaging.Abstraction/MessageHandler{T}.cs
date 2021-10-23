namespace Loom.Messaging
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class MessageHandler<T> : IMessageHandler
    {
        public bool Accepts(Message message) => message is null
            ? throw new ArgumentNullException(nameof(message))
            : message.Data switch
            {
                T data => Accepts(new Message<T>(message.Id, data, message.TracingProperties)),
                _ => false,
            };

        protected virtual bool Accepts(Message<T> message) => true;

        public Task Handle(Message message)
        {
            return Handle(message, CancellationToken.None);
        }

        public Task Handle(Message message, CancellationToken cancellationToken) => message is null
            ? throw new ArgumentNullException(nameof(message))
            : message.Data switch
            {
                T data => Handle(new Message<T>(message.Id, data, message.TracingProperties), cancellationToken),
                _ => Task.CompletedTask,
            };

        protected abstract Task Handle(Message<T> message, CancellationToken cancellationToken);
    }
}
