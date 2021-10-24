using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Loom.Messaging
{
    public sealed class ImmediateMessageBus : IMessageBus
    {
        private readonly IMessageHandler _handler;

        public ImmediateMessageBus(IMessageHandler handler) => _handler = handler;

        public async Task Send(IEnumerable<Message> messages, string partitionKey)
        {
            if (messages is null)
            {
                throw new ArgumentNullException(nameof(messages));
            }

            CancellationToken cancellationToken = default;

            foreach (Message message in messages)
            {
                if (_handler.Accepts(message))
                {
                    await _handler.Handle(message, cancellationToken)
                                  .ConfigureAwait(continueOnCapturedContext: false);
                }
            }
        }
    }
}
