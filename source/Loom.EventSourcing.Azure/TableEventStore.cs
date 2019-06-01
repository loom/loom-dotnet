namespace Loom.EventSourcing.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;
    using Loom.Messaging;
    using Microsoft.Azure.Cosmos.Table;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using static Newtonsoft.Json.JsonConvert;

    public class TableEventStore : IEventCollector, IEventReader
    {
        private readonly CloudTable _table;
        private readonly TypeResolver _typeResolver;
        private readonly JsonSerializerSettings _jsonSettings;

        public TableEventStore(CloudTable table, TypeResolver typeResolver)
        {
            _table = table;
            _typeResolver = typeResolver;
            _jsonSettings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy(),
                }
            };
        }

        public Task CollectEvents(
            Guid streamId, long firstVersion, IEnumerable<object> events)
        {
            IEnumerable<StreamEvent> query = events.Select((source, index) =>
                new StreamEvent(
                    streamId,
                    version: firstVersion + index,
                    eventType: _typeResolver.ResolveTypeName(source.GetType()),
                    payload: SerializeObject(source, _jsonSettings)));

            var batch = new TableBatchOperation();
            foreach (StreamEvent streamEvent in query)
            {
                batch.Insert(streamEvent);
            }

            return _table.ExecuteBatchAsync(batch);
        }

        public async Task<IEnumerable<object>> QueryEvents(
            Guid streamId, long fromVersion)
        {
            TableQuery<StreamEvent> query =
                StreamEvent.CreateQuery(streamId, fromVersion);

            var events = new List<object>();
            TableContinuationToken continuation = default;
            do
            {
                TableQuerySegment<StreamEvent> segment = await _table
                    .ExecuteQuerySegmentedAsync(query, continuation)
                    .ConfigureAwait(false);
                events.AddRange(segment.Select(DeserializeEvent));
                continuation = segment.ContinuationToken;
            }
            while (continuation != default);

            return events.ToImmutableList();
        }

        private object DeserializeEvent(StreamEvent streamEvent)
        {
            return DeserializeObject(
                value: streamEvent.Payload,
                type: _typeResolver.TryResolveType(streamEvent.EventType),
                settings: _jsonSettings);
        }
    }
}
