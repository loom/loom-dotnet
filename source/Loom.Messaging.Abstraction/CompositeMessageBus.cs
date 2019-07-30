namespace Loom.Messaging
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;

    public class CompositeMessageBus : IMessageBus
    {
        private readonly IEnumerable<IMessageBus> _buses;

        public CompositeMessageBus(params IMessageBus[] buses)
            => _buses = new ReadOnlyCollection<IMessageBus>(buses);

        public Task Send(IEnumerable<Message> messages, string partitionKey)
            => Task.WhenAll(_buses.Select(x => x.Send(messages, partitionKey)));
    }
}
