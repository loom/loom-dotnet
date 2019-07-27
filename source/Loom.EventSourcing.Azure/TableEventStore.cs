namespace Loom.EventSourcing.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Loom.Messaging;
    using Microsoft.Azure.Cosmos.Table;
    using Newtonsoft.Json;

    public class TableEventStore<T> : IEventStore<T>, IEventCollector, IEventReader
    {
        private readonly CloudTable _table;
        private readonly TypeResolver _typeResolver;
        private readonly IMessageBus _eventBus;

        public TableEventStore(CloudTable table, TypeResolver typeResolver, IMessageBus eventBus)
        {
            _table = table;
            _typeResolver = typeResolver;
            _eventBus = eventBus;
        }

        public Task CollectEvents(Guid streamId,
                                  long startVersion,
                                  IEnumerable<object> events,
                                  TracingProperties tracingProperties = default)
        {
            return SaveAndPublish(stateType: _typeResolver.ResolveTypeName<T>(),
                                  transaction: Guid.NewGuid(),
                                  streamId,
                                  startVersion,
                                  events.ToList().AsReadOnly(),
                                  tracingProperties);
        }

        private async Task SaveAndPublish(string stateType,
                                          Guid transaction,
                                          Guid streamId,
                                          long startVersion,
                                          IReadOnlyList<object> events,
                                          TracingProperties tracingProperties)
        {
            if (events.Count == 0)
            {
                return;
            }

            await SaveQueueTicket().ConfigureAwait(continueOnCapturedContext: false);
            await SaveEvents().ConfigureAwait(continueOnCapturedContext: false);
            await PublishPendingEvents().ConfigureAwait(continueOnCapturedContext: false);

            Task SaveQueueTicket()
            {
                var queueTicket = new QueueTicket(stateType, streamId, startVersion, events.Count, transaction);
                return _table.ExecuteAsync(TableOperation.Insert(queueTicket));
            }

            Task SaveEvents()
            {
                var batch = new TableBatchOperation();

                for (int i = 0; i < events.Count; i++)
                {
                    object source = events[i];

                    var streamEvent = new StreamEvent(
                        stateType,
                        streamId,
                        version: startVersion + i,
                        raisedTimeUtc: DateTime.UtcNow,
                        eventType: _typeResolver.ResolveTypeName(source.GetType()),
                        payload: JsonConvert.SerializeObject(source),
                        messageId: $"{Guid.NewGuid()}",
                        tracingProperties.OperationId,
                        tracingProperties.Contributor,
                        tracingProperties.ParentId,
                        transaction);

                    batch.Insert(streamEvent);
                }

                return _table.ExecuteBatchAsync(batch);
            }

            async Task PublishPendingEvents()
            {
                TableQuery<QueueTicket> query = QueueTicket.CreateQuery(stateType, streamId).OrderBy("RowKey");
                foreach (QueueTicket queueTicket in await _table.ExecuteTableQuery(query).ConfigureAwait(continueOnCapturedContext: false))
                {
                    await PublishEvents(queueTicket).ConfigureAwait(continueOnCapturedContext: false);
                }
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

        public async Task<IEnumerable<object>> QueryEvents(
            Guid streamId, long fromVersion)
        {
            string stateType = _typeResolver.ResolveTypeName<T>();
            TableQuery<StreamEvent> query = StreamEvent.CreateQuery(stateType, streamId, fromVersion);
            IEnumerable<StreamEvent> streamEvents = await _table.ExecuteTableQuery(query).ConfigureAwait(continueOnCapturedContext: false);
            return streamEvents.Select(DeserializeEvent).ToList().AsReadOnly();
        }

        private object DeserializeEvent(StreamEvent streamEvent)
        {
            return JsonConvert.DeserializeObject(
                value: streamEvent.Payload,
                type: _typeResolver.TryResolveType(streamEvent.EventType));
        }
    }
}
