using System.Collections.Generic;
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

        public bool Accepts(Message message)
            => _handlers.Any(x => x.Accepts(message));

        public Task Handle(Message message, CancellationToken cancellationToken = default)
        {
            IEnumerable<Task> tasks =
                from handler in _handlers
                where handler.Accepts(message)
                select handler.Handle(message, cancellationToken);

            return Task.WhenAll(tasks);
        }
    }
}
