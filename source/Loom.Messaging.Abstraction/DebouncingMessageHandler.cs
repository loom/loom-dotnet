namespace Loom.Messaging
{
    using System;
    using System.Threading.Tasks;

    public sealed class DebouncingMessageHandler : IMessageHandler
    {
        private readonly IDebouncer _debouncer;
        private readonly IMessageHandler _handler;

        public DebouncingMessageHandler(
            IDebouncer debouncer, IMessageHandler handler)
        {
            _debouncer = debouncer;
            _handler = handler;
        }

        public bool Accepts(Message message) => _handler.Accepts(message);

        public Task Handle(Message message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return message.Data is IDebouncable debouncable
                ? _debouncer.TryConsume(debouncable, _ => _handler.Handle(message))
                : _handler.Handle(message);
        }
    }
}
