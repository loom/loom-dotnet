﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Loom.Json;
using Loom.Messaging;
using Microsoft.Azure.Cosmos.Table;

namespace Loom.EventSourcing.Azure
{
    internal sealed class EventPublisher
    {
        private readonly CloudTable _table;
        private readonly TypeResolver _typeResolver;
        private readonly IJsonProcessor _jsonProcessor;
        private readonly IMessageBus _eventBus;

        public EventPublisher(CloudTable table,
                              TypeResolver typeResolver,
                              IJsonProcessor jsonProcessor,
                              IMessageBus eventBus)
        {
            _table = table;
            _typeResolver = typeResolver;
            _jsonProcessor = jsonProcessor;
            _eventBus = eventBus;
        }

        public async Task PublishEvents(
            string stateType,
            string streamId,
            CancellationToken cancellationToken = default)
        {
            IQueryable<QueueTicket> query = _table.BuildQueueTicketsQuery(stateType, streamId);
            foreach (QueueTicket queueTicket in from t in await query.ExecuteAsync(cancellationToken)
                                                                     .ConfigureAwait(continueOnCapturedContext: false)
                                                orderby t.RowKey
                                                select t)
            {
                await FlushEvents(queueTicket, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        private async Task FlushEvents(QueueTicket queueTicket, CancellationToken cancellationToken)
        {
            IQueryable<StreamEvent> query = BuildStreamEventsQuery(queueTicket);
            await PublishStreamEvents(query, partitionKey: $"{queueTicket.StreamId}", cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            await DeleteQueueTicket(queueTicket, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        }

        private IQueryable<StreamEvent> BuildStreamEventsQuery(QueueTicket queueTicket)
        {
            return _table.BuildStreamEventQuery(queueTicket);
        }

        private async Task PublishStreamEvents(
            IQueryable<StreamEvent> query,
            string partitionKey,
            CancellationToken cancellationToken)
        {
            IEnumerable<StreamEvent> source = await query.ExecuteAsync(cancellationToken)
                                                         .ConfigureAwait(continueOnCapturedContext: false);
            await _eventBus.Send(source.Select(GenerateMessage), partitionKey).ConfigureAwait(continueOnCapturedContext: false);
        }

        private Message GenerateMessage(StreamEvent entity)
        {
            return entity.GenerateMessage(_typeResolver, _jsonProcessor);
        }

        private Task DeleteQueueTicket(QueueTicket queueTicket, CancellationToken cancellationToken)
        {
            return _table.ExecuteAsync(TableOperation.Delete(queueTicket), cancellationToken);
        }
    }
}
