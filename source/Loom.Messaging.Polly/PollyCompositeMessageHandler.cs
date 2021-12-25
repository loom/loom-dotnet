using System.Linq;
using Polly;

namespace Loom.Messaging
{
    public class PollyCompositeMessageHandler : CompositeMessageHandler
    {
        public PollyCompositeMessageHandler(
            IAsyncPolicy policy,
            params IMessageHandler[] handlers)
            : base(handlers.Select(handler => new PollyMessageHandler(policy, handler)).ToArray())
        {
        }
    }
}
