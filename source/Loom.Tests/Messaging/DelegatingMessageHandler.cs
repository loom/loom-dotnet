using System;
using System.Threading;
using System.Threading.Tasks;

namespace Loom.Messaging
{
    public sealed class DelegatingMessageHandler : IMessageHandler
    {
        private readonly Func<Message, Task> _handle;

        public DelegatingMessageHandler(Func<Message, Task> handle)
        {
            _handle = handle;
        }

        public bool Accepts(Message message) => true;

        public Task Handle(Message message, CancellationToken cancellationToken)
            => _handle.Invoke(message);
    }
}
