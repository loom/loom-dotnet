namespace Loom.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

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

            foreach (Message message in messages)
            {
                if (_handler.Accepts(message))
                {
                    await _handler.Handle(message).ConfigureAwait(continueOnCapturedContext: false);
                }
            }
        }
    }
}
