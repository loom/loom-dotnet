using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

namespace Loom.Messaging.Azure
{
    public sealed class EventHubMessageBus : IMessageBus
    {
        private readonly EventHubProducerClient _producer;
        private readonly IEventConverter _converter;

        public EventHubMessageBus(EventHubProducerClient producer, IEventConverter converter)
        {
            _producer = producer;
            _converter = converter;
        }

        public async Task Send(
            IEnumerable<Message> messages,
            string partitionKey,
            CancellationToken cancellationToken = default)
        {
            var options = new CreateBatchOptions { PartitionKey = partitionKey };

            EventDataBatch batch = await _producer
                .CreateBatchAsync(options, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);

            foreach (EventData eventData in messages.Select(_converter.ConvertToEvent))
            {
                if (TryGetLocale() is string locale)
                {
                    eventData.Properties["Locale"] = locale;
                }

                if (batch.TryAdd(eventData) == false)
                {
                    await _producer
                        .SendAsync(batch, cancellationToken)
                        .ConfigureAwait(continueOnCapturedContext: false);

                    batch = await _producer
                        .CreateBatchAsync(options, cancellationToken)
                        .ConfigureAwait(continueOnCapturedContext: false);

                    batch.TryAdd(eventData);
                }
            }

            if (batch.Count > 0)
            {
                await _producer
                    .SendAsync(batch, cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        private static string? TryGetLocale()
        {
            if (CultureInfo.CurrentUICulture != CultureInfo.InvariantCulture)
            {
                return CultureInfo.CurrentUICulture.Name;
            }
            else if (CultureInfo.CurrentCulture != CultureInfo.InvariantCulture)
            {
                return CultureInfo.CurrentCulture.Name;
            }
            else
            {
                return null;
            }
        }
    }
}
