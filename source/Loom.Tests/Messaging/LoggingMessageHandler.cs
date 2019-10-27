namespace Loom.Messaging
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public sealed class LoggingMessageHandler : IMessageHandler
    {
        private readonly ConcurrentQueue<Message> _log = new ConcurrentQueue<Message>();

        public IEnumerable<Message> Log => _log;

        public bool CanHandle(Message message) => true;

        public Task Handle(Message message)
        {
            _log.Enqueue(message);
            return Task.CompletedTask;
        }
    }
}
