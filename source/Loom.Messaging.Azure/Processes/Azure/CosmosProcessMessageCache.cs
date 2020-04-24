namespace Loom.Messaging.Processes.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Loom.Json;
    using Loom.Messaging;
    using Loom.Messaging.Azure;
    using Loom.Messaging.Processes;
    using Microsoft.Azure.Cosmos;
    using Microsoft.Azure.Cosmos.Fluent;
    using Microsoft.Azure.Cosmos.Linq;

    public class CosmosProcessMessageCache
        : IProcessEventReader, IProcessEventCollector
    {
        private readonly Container _container;
        private readonly IJsonProcessor _jsonProcessor;
        private readonly TypeResolver _typeResolver;

        public CosmosProcessMessageCache(
            string connectionString,
            string databaseId,
            string containerId,
            IJsonProcessor jsonProcessor,
            TypeResolver typeResolver)
        {
            _container = CreateContainer(connectionString, databaseId, containerId);
            _jsonProcessor = jsonProcessor;
            _typeResolver = typeResolver;
        }

        private static Container CreateContainer(
            string connectionString, string databaseId, string containerId)
        {
            var builder = new CosmosClientBuilder(connectionString);
            var options = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
            };
            CosmosClient client = builder.WithSerializerOptions(options).Build();
            return client.GetContainer(databaseId, containerId);
        }

        public async Task<IEnumerable<Message>> Query(
            string operationId, CancellationToken cancellationToken)
        {
            var entities = new List<ProcessEventMessage>();

            IQueryable<ProcessEventMessage> query =
                from item in _container.GetItemLinqQueryable<ProcessEventMessage>()
                where item.OperationId == operationId
                select item;

            var iterator = query.ToFeedIterator();

            while (iterator.HasMoreResults)
            {
                FeedResponse<ProcessEventMessage> response = await iterator
                    .ReadNextAsync(cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);

                entities.AddRange(response.Resource);
            }

            // TODO: if type cannot be resolved throw an exception.
            IEnumerable<Message> messages =
                from e in entities
                let type = _typeResolver.TryResolveType(e.DataType)
                let data = _jsonProcessor.FromJson(e.DataJson, type)
                let tracingProperties = new TracingProperties(e.OperationId, e.Contributor, e.ParentId)
                select new Message(e.Id, data, tracingProperties);

            return messages.ToList().AsReadOnly();
        }

        public Task Collect(Message message, CancellationToken cancellationToken)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return _container.CreateItemAsync(new ProcessEventMessage(
                id: $"{message.Id}",
                dataType: _typeResolver.ResolveTypeName(message.Data.GetType()),
                dataJson: _jsonProcessor.ToJson(message.Data),
                operationId: $"{message.TracingProperties.OperationId}",
                contributor: $"{message.TracingProperties.Contributor}",
                parentId: $"{message.TracingProperties.ParentId}"));
        }
    }
}
