using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Loom.Messaging
{
    public class CompositeMessageHandler : IMessageHandler
    {
        private readonly ReadOnlyCollection<IMessageHandler> _handlers;

        public CompositeMessageHandler(params IMessageHandler[] handlers)
            => _handlers = handlers.ToList().AsReadOnly();

        public bool CanHandle(Message message)
            => _handlers.Any(x => x.CanHandle(message));

        public Task Handle(Message message, CancellationToken cancellationToken = default)
        {
            return Task.WhenAll(_handlers.Select(TryHandle));

            async Task TryHandle(IMessageHandler handler)
            {
                if (handler.CanHandle(message))
                {
                    await handler.Handle(message, cancellationToken)
                                 .ConfigureAwait(continueOnCapturedContext: false);
                }
            }
        }
    }
}
