using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Loom.Messaging
{
    public interface IMessageBus
    {
        // TODO: Remove default argument of parameter cancellationToken.
        Task Send(
            IEnumerable<Message> messages,
            string partitionKey,
            CancellationToken cancellationToken = default);
    }
}
