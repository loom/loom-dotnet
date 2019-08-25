namespace Loom.EventSourcing.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Loom.EventSourcing.Serialization;
    using Loom.Messaging;
    using Microsoft.Azure.Cosmos.Table;

    public class TableEventStore<T> : IEventStore<T>, IEventCollector, IEventReader
    {
        private readonly CloudTable _table;
        private readonly TypeResolver _typeResolver;
        private readonly IJsonSerializer _serializer;
        private readonly EventPublisher _publisher;

        public TableEventStore(CloudTable table,
                               TypeResolver typeResolver,
                               IJsonSerializer serializer,
                               IMessageBus eventBus)
        {
            _table = table;
            _typeResolver = typeResolver;
            _serializer = serializer;
            _publisher = new EventPublisher(table, typeResolver, serializer, eventBus);
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
                        payload: _serializer.Serialize(source),
                        messageId: $"{Guid.NewGuid()}",
                        tracingProperties.OperationId,
                        tracingProperties.Contributor,
                        tracingProperties.ParentId,
                        transaction);

                    batch.Insert(streamEvent);
                }

                return _table.ExecuteBatchAsync(batch);
            }

            Task PublishPendingEvents() => _publisher.PublishEvents(stateType, streamId);
        }

        public async Task<IEnumerable<object>> QueryEvents(Guid streamId, long fromVersion)
        {
            string stateType = _typeResolver.ResolveTypeName<T>();
            IQueryable<StreamEvent> query = _table.BuildStreamEventsQuery(stateType, streamId, fromVersion);
            IEnumerable<StreamEvent> streamEvents = await query.ExecuteAsync().ConfigureAwait(continueOnCapturedContext: false);
            return streamEvents.Select(DeserializeEvent).ToList().AsReadOnly();
        }

        private object DeserializeEvent(StreamEvent streamEvent)
        {
            return streamEvent.DeserializePayload(_typeResolver, _serializer);
        }
    }
}
