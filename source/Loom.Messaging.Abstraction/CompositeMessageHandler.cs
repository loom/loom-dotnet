namespace Loom.Messaging
{
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;

    public class CompositeMessageHandler : IMessageHandler
    {
        private readonly ReadOnlyCollection<IMessageHandler> _handlers;

        public CompositeMessageHandler(params IMessageHandler[] handlers)
            => _handlers = handlers.ToList().AsReadOnly();

        public bool CanHandle(Message message)
            => _handlers.Any(x => x.CanHandle(message));

        public Task Handle(Message message)
            => Task.WhenAll(_handlers.Where(x => x.CanHandle(message)).Select(x => x.Handle(message)));
    }
}
