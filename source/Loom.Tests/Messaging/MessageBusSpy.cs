namespace Loom.Messaging
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Threading.Tasks;

    public sealed class MessageBusSpy : IMessageBus
    {
        private readonly ConcurrentQueue<ImmutableArray<Message>> _calls;

        public MessageBusSpy()
        {
            _calls = new ConcurrentQueue<ImmutableArray<Message>>();
        }

        public IEnumerable<ImmutableArray<Message>> Calls => _calls;

        public Task Send(IEnumerable<Message> messages)
        {
            _calls.Enqueue(messages.ToImmutableArray());
            return Task.CompletedTask;
        }
    }
}
