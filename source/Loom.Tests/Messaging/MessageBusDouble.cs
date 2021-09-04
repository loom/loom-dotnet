using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Loom.Messaging
{
    public sealed class MessageBusDouble : IMessageBus
    {
        private readonly ConcurrentQueue<(ImmutableArray<Message>, string)> _calls;
        private readonly ConcurrentQueue<Exception> _errors;

        public MessageBusDouble(IEnumerable<Exception> errors)
        {
            _calls = new ConcurrentQueue<(ImmutableArray<Message>, string)>();
            _errors = new ConcurrentQueue<Exception>(errors);
        }

        public MessageBusDouble(int errors)
            : this(Enumerable.Range(0, errors).Select(_ => new InvalidOperationException()))
        {
        }

        public MessageBusDouble()
            : this(Array.Empty<Exception>())
        {
        }

        public IEnumerable<(ImmutableArray<Message> Messages, string PartitionKey)> Calls => _calls;

        public async Task Send(IEnumerable<Message> messages, string partitionKey)
        {
            _calls.Enqueue((messages.ToImmutableArray(), partitionKey));
            await Task.Delay(millisecondsDelay: 1);
            if (_errors.TryDequeue(out Exception error))
            {
                throw error;
            }
        }

        public void Clear() => _calls.Clear();
    }
}
