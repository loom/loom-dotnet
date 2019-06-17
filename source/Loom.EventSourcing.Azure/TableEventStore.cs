﻿namespace Loom.EventSourcing.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Loom.Messaging;
    using Microsoft.Azure.Cosmos.Table;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    public class TableEventStore : IEventCollector, IEventReader
    {
        private readonly CloudTable _table;
        private readonly TypeResolver _typeResolver;
        private readonly IMessageBus _eventBus;
        private readonly JsonSerializerSettings _jsonSettings;

        public TableEventStore(CloudTable table, TypeResolver typeResolver, IMessageBus eventBus)
        {
            _table = table;
            _typeResolver = typeResolver;
            _eventBus = eventBus;
            _jsonSettings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy(),
                },
            };
        }

        public Task CollectEvents(Guid streamId,
                                  long startVersion,
                                  IEnumerable<object> events,
                                  TracingProperties tracingProperties = default)
        {
            return CollectEventsWith(transaction: Guid.NewGuid(),
                                     streamId,
                                     startVersion,
                                     events.ToImmutableArray(),
                                     tracingProperties);
        }

        private async Task CollectEventsWith(Guid transaction,
                                             Guid streamId,
                                             long startVersion,
                                             ImmutableArray<object> events,
                                             TracingProperties tracingProperties)
        {
            await SaveQueueTicket().ConfigureAwait(continueOnCapturedContext: false);
            await SaveStreamEvents().ConfigureAwait(continueOnCapturedContext: false);
            await PublishStreamEvents().ConfigureAwait(continueOnCapturedContext: false);

            Task SaveQueueTicket()
            {
                var queueTicket = new QueueTicket(streamId, startVersion, events.Length, transaction);
                return _table.ExecuteAsync(TableOperation.Insert(queueTicket));
            }

            Task SaveStreamEvents()
            {
                var batch = new TableBatchOperation();

                for (int i = 0; i < events.Length; i++)
                {
                    object source = events[i];

                    var streamEvent = new StreamEvent(
                        streamId,
                        version: startVersion + i,
                        eventType: _typeResolver.ResolveTypeName(source.GetType()),
                        payload: JsonConvert.SerializeObject(source, _jsonSettings),
                        messageId: $"{Guid.NewGuid()}",
                        tracingProperties.OperationId,
                        tracingProperties.Contributor,
                        tracingProperties.ParentId,
                        transaction);

                    batch.Insert(streamEvent);
                }

                return _table.ExecuteBatchAsync(batch);
            }

            async Task PublishStreamEvents()
            {
                TableQuery<QueueTicket> query = QueueTicket.CreateQuery(streamId).OrderBy("RowKey");
                foreach (QueueTicket queueTicket in await ExecuteQuery(query).ConfigureAwait(continueOnCapturedContext: false))
                {
                    await PublishStreamEventsWith(queueTicket).ConfigureAwait(continueOnCapturedContext: false);
                }
            }
        }

        private async Task PublishStreamEventsWith(QueueTicket queueTicket)
        {
            TableQuery<StreamEvent> query = StreamEvent.CreateQuery(queueTicket);
            IEnumerable<StreamEvent> streamEvents = await ExecuteQuery(query).ConfigureAwait(continueOnCapturedContext: false);
            await _eventBus.Send(streamEvents.Select(GenerateMessage)).ConfigureAwait(continueOnCapturedContext: false);
            await _table.ExecuteAsync(TableOperation.Delete(queueTicket)).ConfigureAwait(continueOnCapturedContext: false);
        }

        private Message GenerateMessage(StreamEvent streamEvent)
        {
            return new Message(
                id: streamEvent.MessageId,
                data: RestoreStreamEvent(entity: streamEvent),
                streamEvent.TracingProperties);
        }

        private object RestoreStreamEvent(StreamEvent entity)
        {
            Type type = _typeResolver.TryResolveType(entity.EventType);

            ConstructorInfo constructor = typeof(StreamEvent<>)
                .MakeGenericType(type)
                .GetTypeInfo()
                .GetConstructor(new[] { typeof(Guid), typeof(long), type });

            return constructor.Invoke(parameters: new object[]
            {
                entity.StreamId,
                entity.Version,
                JsonConvert.DeserializeObject(entity.Payload, type, _jsonSettings),
            });
        }

        public async Task<IEnumerable<object>> QueryEvents(
            Guid streamId, long fromVersion)
        {
            TableQuery<StreamEvent> query = StreamEvent.CreateQuery(streamId, fromVersion);
            IEnumerable<StreamEvent> streamEvents = await ExecuteQuery(query).ConfigureAwait(continueOnCapturedContext: false);
            return streamEvents.Select(DeserializeEvent).ToImmutableArray();
        }

        private async Task<IEnumerable<T>> ExecuteQuery<T>(TableQuery<T> query)
             where T : ITableEntity, new()
        {
            var results = new List<T>();

            TableContinuationToken continuation = default;
            do
            {
                TableQuerySegment<T> segment = await _table.ExecuteQuerySegmentedAsync(query, continuation).ConfigureAwait(continueOnCapturedContext: false);
                results.AddRange(segment);
                continuation = segment.ContinuationToken;
            }
            while (continuation != default);

            return results.ToImmutableList();
        }

        private object DeserializeEvent(StreamEvent streamEvent)
        {
            return JsonConvert.DeserializeObject(
                value: streamEvent.Payload,
                type: _typeResolver.TryResolveType(streamEvent.EventType),
                settings: _jsonSettings);
        }
    }
}
