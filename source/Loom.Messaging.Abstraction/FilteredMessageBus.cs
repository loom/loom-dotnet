namespace Loom.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public sealed class FilteredMessageBus : IMessageBus
    {
        private readonly Func<Message, bool> _predicate;
        private readonly IMessageBus _bus;

        public FilteredMessageBus(Func<Message, bool> predicate, IMessageBus bus)
        {
            _predicate = predicate;
            _bus = bus;
        }

        public Task Send(IEnumerable<Message> messages, string partitionKey)
        {
            return _bus.Send(messages.Where(_predicate).ToList().AsReadOnly(), partitionKey);
        }
    }
}
