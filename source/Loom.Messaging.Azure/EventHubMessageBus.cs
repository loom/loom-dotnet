namespace Loom.Messaging.Azure
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Loom.Json;
    using Microsoft.Azure.EventHubs;

    public sealed class EventHubMessageBus : IMessageBus
    {
        private readonly EventHubClient _eventHub;
        private readonly IJsonProcessor _jsonProcessor;
        private readonly TypeResolver _typeResolver;

        public EventHubMessageBus(EventHubClient eventHub,
                                  IJsonProcessor jsonProcessor,
                                  TypeResolver typeResolver)
        {
            _eventHub = eventHub;
            _jsonProcessor = jsonProcessor;
            _typeResolver = typeResolver;
        }

        public async Task Send(IEnumerable<Message> messages,
                               string partitionKey)
        {
            var options = new BatchOptions { PartitionKey = partitionKey };

            EventDataBatch batch = _eventHub.CreateBatch(options);

            foreach (EventData eventData in messages.Select(GetEvent).ToArray())
            {
                if (batch.TryAdd(eventData) == false)
                {
                    await _eventHub.SendAsync(batch).ConfigureAwait(continueOnCapturedContext: false);
                    batch = _eventHub.CreateBatch(options);
                    batch.TryAdd(eventData);
                }
            }

            await _eventHub.SendAsync(batch).ConfigureAwait(continueOnCapturedContext: false);
        }

        private EventData GetEvent(Message message)
        {
            object data = message.Data;
            TracingProperties tracingProperties = message.TracingProperties;
            byte[] array = Encoding.UTF8.GetBytes(_jsonProcessor.ToJson(data));
            return new EventData(array)
            {
                Properties =
                {
                    ["Id"] = message.Id,
                    ["Type"] = _typeResolver.ResolveTypeName(data.GetType()),
                    ["OperationId"] = tracingProperties.OperationId,
                    ["Contributor"] = tracingProperties.Contributor,
                    ["ParentId"] = tracingProperties.ParentId,
                },
            };
        }
    }
}
