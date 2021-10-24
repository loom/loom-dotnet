using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Loom.Messaging
{
    public class CompositeMessageBus : IMessageBus
    {
        private readonly IEnumerable<IMessageBus> _buses;

        public CompositeMessageBus(params IMessageBus[] buses)
            => _buses = new ReadOnlyCollection<IMessageBus>(buses);

        public Task Send(
            IEnumerable<Message> messages,
            string partitionKey,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<Task> tasks =
                from bus in _buses
                select bus.Send(messages, partitionKey, cancellationToken);

            return Task.WhenAll(tasks);
        }
    }
}
