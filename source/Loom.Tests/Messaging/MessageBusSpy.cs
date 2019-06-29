namespace Loom.Messaging
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Threading.Tasks;

    public sealed class MessageBusSpy : IMessageBus
    {
        private readonly ConcurrentQueue<(ImmutableArray<Message>, string)> _calls;

        public MessageBusSpy()
        {
            _calls = new ConcurrentQueue<(ImmutableArray<Message>, string)>();
        }

        public IEnumerable<(ImmutableArray<Message> messages, string partitionKey)> Calls => _calls;

        public Task Send(IEnumerable<Message> messages)
        {
            _calls.Enqueue((messages.ToImmutableArray(), null));
            return Task.CompletedTask;
        }

        public Task Send(IEnumerable<Message> messages, string partitionKey)
        {
            _calls.Enqueue((messages.ToImmutableArray(), partitionKey));
            return Task.CompletedTask;
        }

        public void Clear() => _calls.Clear();
    }
}
