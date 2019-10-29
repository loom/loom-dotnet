namespace Loom.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.EventHubs;
    using Microsoft.Extensions.Logging;

    public sealed class EventProcessor
    {
        private readonly IEventConverter _converter;
        private readonly IMessageHandler _handler;
        private readonly ILogger _logger;

        internal EventProcessor(
            IEventConverter converter, IMessageHandler handler, ILogger logger)
        {
            _converter = converter;
            _handler = handler;
            _logger = logger;
        }

        public async Task Process(IEnumerable<EventData> events)
        {
            if (events is null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            foreach (EventData eventData in events)
            {
                await ProcessEvent(eventData).ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        private async Task ProcessEvent(EventData eventData)
        {
            try
            {
                await TryConvertThenHandle(eventData).ConfigureAwait(continueOnCapturedContext: false);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Event processing failed. See the exception for details.");
            }
        }

        private async Task TryConvertThenHandle(EventData eventData)
        {
            if (_converter.TryConvertToMessage(eventData) is Message message)
            {
                await TryHandleMessage(message).ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        private async Task TryHandleMessage(Message message)
        {
            if (_handler.CanHandle(message))
            {
                await HandleMessage(message).ConfigureAwait(continueOnCapturedContext: false);
            }
            else
            {
                _logger.LogTrace($"The message '{message.Id}' with data type '{message.Data.GetType().FullName}' is unhandleable.");
            }
        }

        private async Task HandleMessage(Message message)
        {
            await _handler.Handle(message).ConfigureAwait(continueOnCapturedContext: false);
            _logger.LogTrace($"The message '{message.Id}' with data type '{message.Data.GetType().FullName}' was handled successfully.");
        }
    }
}
