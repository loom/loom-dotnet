namespace Loom.Messaging.Azure
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.EventHubs;

    public sealed class EventHubMessageBus : IMessageBus
    {
        private readonly EventHubClient _eventHub;
        private readonly IEventConverter _converter;

        public EventHubMessageBus(EventHubClient eventHub, IEventConverter converter)
        {
            _eventHub = eventHub;
            _converter = converter;
        }

        public async Task Send(IEnumerable<Message> messages, string partitionKey)
        {
            var options = new BatchOptions { PartitionKey = partitionKey };

            EventDataBatch batch = _eventHub.CreateBatch(options);

            foreach (EventData eventData in messages.Select(_converter.ConvertToEvent))
            {
                if (batch.TryAdd(eventData) == false)
                {
                    await _eventHub.SendAsync(batch).ConfigureAwait(continueOnCapturedContext: false);
                    batch = _eventHub.CreateBatch(options);
                    batch.TryAdd(eventData);
                }
            }

            if (batch.Count > 0)
            {
                await _eventHub.SendAsync(batch).ConfigureAwait(continueOnCapturedContext: false);
            }
        }
    }
}
