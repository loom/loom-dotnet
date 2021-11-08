using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Loom.Messaging
{
    public sealed class LoggingMessageHandler : IMessageHandler
    {
        private readonly ConcurrentQueue<Message> _log = new();

        public IEnumerable<Message> Log => _log;

        public bool CanHandle(Message message) => true;

        public Task Handle(Message message, CancellationToken cancellationToken)
        {
            _log.Enqueue(message);
            return Task.CompletedTask;
        }
    }
}
