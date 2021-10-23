using System.Collections.Generic;
using System.Threading.Tasks;

namespace Loom.Messaging
{
    public interface IMessageBus
    {
        // TODO: Add a parameter of CancellationToken.
        Task Send(IEnumerable<Message> messages, string partitionKey);
    }
}
