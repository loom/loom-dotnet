namespace Loom.Messaging
{
    using System.Threading.Tasks;

    // TODO: Should be unit tested.
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

        public bool CanHandle(Message message) => _handler.CanHandle(message);

        public Task Handle(Message message)
            => message?.Data is IDebouncable debouncable
            ? _debouncer.TryConsume(debouncable, _ => _handler.Handle(message))
            : _handler.Handle(message);
    }
}
