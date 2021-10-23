namespace Loom.Messaging
{
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;

    public class SequentialCompositeMessageHandler : IMessageHandler
    {
        private readonly ReadOnlyCollection<IMessageHandler> _handlers;

        public SequentialCompositeMessageHandler(params IMessageHandler[] handlers)
            => _handlers = handlers.ToList().AsReadOnly();

        public bool Accepts(Message message)
            => _handlers.Any(x => x.Accepts(message));

        public async Task Handle(Message message)
        {
            foreach (IMessageHandler handler in _handlers)
            {
                if (handler.Accepts(message))
                {
                    await handler.Handle(message).ConfigureAwait(continueOnCapturedContext: false);
                }
            }
        }
    }
}
