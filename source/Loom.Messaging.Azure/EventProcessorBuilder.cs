namespace Loom.Messaging.Azure
{
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

        public EventProcessor Build()
        {
            return new EventProcessor(_converter, _handler);
        }
    }
}
