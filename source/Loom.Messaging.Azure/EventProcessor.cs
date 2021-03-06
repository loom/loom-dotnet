﻿namespace Loom.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.EventHubs;

    public sealed class EventProcessor
    {
        private readonly IEventConverter _converter;
        private readonly IMessageHandler _handler;

        internal EventProcessor(IEventConverter converter, IMessageHandler handler)
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

            List<Exception>? exceptions = default;

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
                    switch (exceptions)
                    {
                        case null:
                            exceptions = new List<Exception> { exception };
                            break;

                        default:
                            exceptions.Add(exception);
                            break;
                    }
                }
            }

            if (exceptions != null)
            {
                throw new AggregateException(innerExceptions: exceptions);
            }
        }
    }
}
