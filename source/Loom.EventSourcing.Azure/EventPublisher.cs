namespace Loom.EventSourcing.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Loom.EventSourcing.Serialization;
    using Loom.Messaging;
    using Microsoft.Azure.Cosmos.Table;

    internal sealed class EventPublisher
    {
        private readonly CloudTable _table;
        private readonly TypeResolver _typeResolver;
        private readonly IJsonSerializer _serializer;
        private readonly IMessageBus _eventBus;

        public EventPublisher(CloudTable table,
                              TypeResolver typeResolver,
                              IJsonSerializer serializer,
                              IMessageBus eventBus)
        {
            _table = table;
            _typeResolver = typeResolver;
            _serializer = serializer;
            _eventBus = eventBus;
        }

        public async Task PublishEvents(string stateType, Guid streamId)
        {
            IQueryable<QueueTicket> query = _table.BuildQueueTicketsQuery(stateType, streamId);
            foreach (QueueTicket queueTicket in from t in await query.ExecuteAsync().ConfigureAwait(continueOnCapturedContext: false)
                                                orderby t.RowKey
                                                select t)
            {
                await FlushEvents(queueTicket).ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        private async Task FlushEvents(QueueTicket queueTicket)
        {
            IQueryable<StreamEvent> query = BuildStreamEventsQuery(queueTicket);
            await PublishStreamEvents(query, partitionKey: $"{queueTicket.StreamId}").ConfigureAwait(continueOnCapturedContext: false);
            await DeleteQueueTicket(queueTicket).ConfigureAwait(continueOnCapturedContext: false);
        }

        private IQueryable<StreamEvent> BuildStreamEventsQuery(QueueTicket queueTicket)
        {
            return _table.BuildStreamEventsQuery(queueTicket);
        }

        private async Task PublishStreamEvents(IQueryable<StreamEvent> query, string partitionKey)
        {
            IEnumerable<StreamEvent> streamEvents = await query.ExecuteAsync().ConfigureAwait(continueOnCapturedContext: false);
            await _eventBus.Send(streamEvents.Select(GenerateMessage), partitionKey).ConfigureAwait(continueOnCapturedContext: false);
        }

        private Message GenerateMessage(StreamEvent entity)
        {
            return entity.GenerateMessage(_typeResolver, _serializer);
        }

        private Task DeleteQueueTicket(QueueTicket queueTicket)
        {
            return _table.ExecuteAsync(TableOperation.Delete(queueTicket));
        }
    }
}
