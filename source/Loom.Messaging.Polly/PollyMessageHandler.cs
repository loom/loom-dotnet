using System.Threading;
using System.Threading.Tasks;
using Polly;

namespace Loom.Messaging
{
    public sealed class PollyMessageHandler : IMessageHandler
    {
        private readonly IAsyncPolicy _policy;
        private readonly IMessageHandler _handler;

        public PollyMessageHandler(IAsyncPolicy policy, IMessageHandler handler)
            => (_policy, _handler) = (policy, handler);

        public bool CanHandle(Message message)
            => _handler.CanHandle(message);

        public Task Handle(Message message, CancellationToken cancellationToken)
            => _policy.ExecuteAsync(() => _handler.Handle(message, cancellationToken));
    }
}
