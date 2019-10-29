namespace Loom.Messaging.Azure
{
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;

    public sealed class EventProcessorBuilder
    {
        private readonly IEventConverter _converter;
        private readonly IMessageHandler _handler;

        public EventProcessorBuilder(
            IEventConverter converter, IMessageHandler handler)
        {
            _converter = converter;
            _handler = handler;
        }

        public EventProcessor Build(ILogger logger)
            => new EventProcessor(_converter, _handler, logger);

        public EventProcessor Build() => Build(NullLogger.Instance);
    }
}
