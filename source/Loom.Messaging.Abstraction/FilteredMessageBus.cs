using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Loom.Messaging
{
    public sealed class FilteredMessageBus : IMessageBus
    {
        private readonly Func<Message, bool> _predicate;
        private readonly IMessageBus _bus;

        public FilteredMessageBus(Func<Message, bool> predicate, IMessageBus bus)
        {
            _predicate = predicate;
            _bus = bus;
        }

        public Task Send(
            IEnumerable<Message> messages,
            string partitionKey,
            CancellationToken cancellationToken = default)
        {
            return _bus.Send(Filter(messages), partitionKey, cancellationToken);
        }

        private IReadOnlyList<Message> Filter(IEnumerable<Message> messages)
            => messages.Where(_predicate).ToList().AsReadOnly();
    }
}
