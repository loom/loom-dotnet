namespace Loom.EventSourcing.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Loom.Messaging;
    using Microsoft.Azure.Cosmos.Table;

    public sealed class TableEventPublisher
    {
        private readonly CloudTable _table;
        private readonly TypeResolver _typeResolver;
        private readonly IMessageBus _eventBus;
        private readonly TimeSpan _minimumPendingTime;

        public TableEventPublisher(CloudTable table,
                                   TypeResolver typeResolver,
                                   IMessageBus eventBus,
                                   TimeSpan minimumPendingTime)
        {
            _table = table;
            _typeResolver = typeResolver;
            _eventBus = eventBus;
            _minimumPendingTime = minimumPendingTime;
        }

        public async Task PublishPendingEvents()
        {
            IQueryable<QueueTicket> query = BuildQueueTicketsQuery();
            foreach (QueueTicket t in await ScanQueueTickets(query).ConfigureAwait(continueOnCapturedContext: false))
            {
                await FlushEvents(t).ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        private IQueryable<QueueTicket> BuildQueueTicketsQuery() => _table.BuildQueueTicketsQuery();

        private async Task<IOrderedEnumerable<QueueTicket>> ScanQueueTickets(IQueryable<QueueTicket> query)
        {
            return from t in await query.ExecuteAsync().ConfigureAwait(continueOnCapturedContext: false)
                   where DateTime.UtcNow - t.Timestamp.UtcDateTime >= _minimumPendingTime
                   orderby t.StartVersion
                   select t;
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

        private Task DeleteQueueTicket(QueueTicket queueTicket)
        {
            return _table.ExecuteAsync(TableOperation.Delete(queueTicket));
        }

        private Message GenerateMessage(StreamEvent entity) => entity.GenerateMessage(_typeResolver);
    }
}
