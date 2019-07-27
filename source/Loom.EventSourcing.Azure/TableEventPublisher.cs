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
            string filter = "PartitionKey ge '~'";
            TableQuery<QueueTicket> query = new TableQuery<QueueTicket>().Where(filter);
            foreach (QueueTicket queueTicket in from t in await _table.ExecuteTableQuery(query).ConfigureAwait(continueOnCapturedContext: false)
                                                where DateTime.UtcNow - t.Timestamp.UtcDateTime >= _minimumPendingTime
                                                orderby t.StartVersion
                                                select t)
            {
                await PublishEvents(queueTicket).ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        private async Task PublishEvents(QueueTicket queueTicket)
        {
            TableQuery<StreamEvent> query = StreamEvent.CreateQuery(queueTicket);
            IEnumerable<StreamEvent> streamEvents = await _table.ExecuteTableQuery(query).ConfigureAwait(continueOnCapturedContext: false);
            string partitionKey = $"{queueTicket.StreamId}";
            await _eventBus.Send(streamEvents.Select(GenerateMessage), partitionKey).ConfigureAwait(continueOnCapturedContext: false);
            await _table.ExecuteAsync(TableOperation.Delete(queueTicket)).ConfigureAwait(continueOnCapturedContext: false);
        }

        private Message GenerateMessage(StreamEvent entity) => entity.GenerateMessage(_typeResolver);
    }
}
