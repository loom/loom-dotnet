namespace Loom.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.EventHubs;

    public sealed class EventProcessor
    {
        private readonly IEventConverter _converter;
        private readonly IMessageHandler _handler;

        internal EventProcessor(
            IEventConverter converter, IMessageHandler handler)
        {
            _converter = converter;
            _handler = handler;
        }

        public async Task Process(IEnumerable<EventData> events)
        {
            if (events is null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            var exceptions = new List<Exception>();

            foreach (EventData eventData in events)
            {
                try
                {
                    if (_converter.TryConvertToMessage(eventData) is Message message)
                    {
                        if (_handler.CanHandle(message))
                        {
                            await _handler.Handle(message).ConfigureAwait(continueOnCapturedContext: false);
                        }
                    }
                }
                catch (Exception exception)
                {
                    exceptions.Add(exception);
                }
            }

            if (exceptions.Any())
            {
                throw new AggregateException(exceptions);
            }
        }
    }
}
