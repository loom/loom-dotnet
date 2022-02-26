using System;
using System.Threading;
using System.Threading.Tasks;

namespace Loom.Messaging
{
    public abstract class MessageHandler<T> : IMessageHandler
        where T : notnull
    {
        public bool CanHandle(Message message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return message.Data is T;
        }

        public Task Handle(
            Message message,
            CancellationToken cancellationToken = default)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return Handle(Transform(message), cancellationToken);
        }

        private static Message<T> Transform(Message source)
        {
            if (source.Data is T data)
            {
                return new Message<T>(
                    source.Id,
                    source.ProcessId,
                    source.Initiator,
                    source.PredecessorId,
                    data);
            }
            else
            {
                throw new InvalidOperationException("Could not handle data of unknown type.");
            }
        }

        protected abstract Task Handle(
            Message<T> message,
            CancellationToken cancellationToken);
    }
}
