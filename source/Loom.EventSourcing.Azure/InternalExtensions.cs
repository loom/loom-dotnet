namespace Loom.EventSourcing.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Loom.Messaging;
    using Microsoft.Azure.Cosmos.Table;
    using Newtonsoft.Json;

    internal static class InternalExtensions
    {
        public static async Task<IEnumerable<TElement>> ExecuteTableQuery<TElement>(
            this CloudTable table, TableQuery<TElement> query)
            where TElement : ITableEntity, new()
        {
            var results = new List<TElement>();

            TableContinuationToken continuation = default;
            do
            {
                TableQuerySegment<TElement> segment = await table
                    .ExecuteQuerySegmentedAsync(query, continuation)
                    .ConfigureAwait(continueOnCapturedContext: false);
                results.AddRange(segment);
                continuation = segment.ContinuationToken;
            }
            while (continuation != default);

            return results.ToList();
        }

        public static Message GenerateMessage(
            this StreamEvent entity, TypeResolver typeResolver)
        {
            Type type = typeResolver.TryResolveType(entity.EventType);

            ConstructorInfo constructor = typeof(StreamEvent<>)
                .MakeGenericType(type)
                .GetTypeInfo()
                .GetConstructor(new[] { typeof(Guid), typeof(long), typeof(DateTime), type });

            object data = constructor.Invoke(parameters: new object[]
            {
                entity.StreamId,
                entity.Version,
                entity.RaisedTimeUtc,
                JsonConvert.DeserializeObject(entity.Payload, type),
            });

            return new Message(id: entity.MessageId, data, entity.TracingProperties);
        }
    }
}
