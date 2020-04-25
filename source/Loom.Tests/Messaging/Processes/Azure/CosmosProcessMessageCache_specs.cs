namespace Loom.Messaging.Processes.Azure
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Loom.Json;
    using Loom.Testing;
    using Microsoft.Azure.Cosmos;
    using Microsoft.Azure.Cosmos.Fluent;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CosmosProcessMessageCache_specs
    {
        private const string ConnectionString = "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        private const string DatabaseId = "UnitTestingDatabase";
        private const string ContainerId = "UnitTestContainer";

        public Container Container { get; set; }

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            try
            {
                var builder = new CosmosClientBuilder(ConnectionString);
                var options = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
                };
                CosmosClient client = builder.WithSerializerOptions(options).Build();
                Database database = client.CreateDatabaseIfNotExistsAsync(DatabaseId).GetAwaiter().GetResult().Database;
                var properties = new ContainerProperties
                {
                    Id = ContainerId,
                    PartitionKeyPath = "/id",
                    DefaultTimeToLive = -1,
                };
                database.CreateContainerIfNotExistsAsync(properties).GetAwaiter().GetResult();
            }
            catch
            {
                Assert.Inconclusive();
            }
        }

        [TestInitialize]
        public void Initialize()
        {
            var builder = new CosmosClientBuilder(ConnectionString);
            var options = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
            };
            CosmosClient client = builder.WithSerializerOptions(options).Build();
            Container = client.GetContainer(DatabaseId, ContainerId);
        }

        [TestMethod]
        public void sut_implements_IProcessMessageReader()
        {
            typeof(CosmosProcessMessageCache).Should().Implement<IProcessMessageReader>();
        }

        [TestMethod]
        public void sut_implements_IProcessMessageCollector()
        {
            typeof(CosmosProcessMessageCache).Should().Implement<IProcessMessageCollector>();
        }

        [TestMethod, AutoData]
        public async Task given_message_then_sut_collects_correctly(
            Message message,
            IJsonProcessor jsonProcessor,
            TypeResolver typeResolver)
        {
            // Arrange
            var sut = new CosmosProcessMessageCache(
                ConnectionString, DatabaseId, ContainerId, jsonProcessor, typeResolver);

            // Actt
            await sut.Collect(message, cancellationToken: CancellationToken.None);

            // Assert
            ItemResponse<dynamic> response =
                await Container.ReadItemAsync<dynamic>($"{message.Id}", new PartitionKey($"{message.Id}"));

            dynamic actual = response.Resource;

            ((object)actual).Should().NotBeNull();

            string id = actual.id;
            id.Should().Be($"{message.Id}");

            string operationId = actual.operationId;
            operationId.Should().Be($"{message.TracingProperties.OperationId}");

            string parentId = actual.parentId;
            parentId.Should().Be($"{message.TracingProperties.ParentId}");

            string contributor = actual.contributor;
            contributor.Should().Be($"{message.TracingProperties.Contributor}");

            string dataType = actual.dataType;
            dataType.Should().Be(typeResolver.ResolveTypeName(message.Data.GetType()));

            string dataJson = actual.dataJson;
            dataJson.Should().Be(jsonProcessor.ToJson(message.Data));
        }

        [TestMethod, AutoData]
        public async Task given_operationId_then_sut_queries_process_events_correctly(
            Message message,
            IJsonProcessor jsonProcessor,
            TypeResolver typeResolver)
        {
            // Arrange
            var sut = new CosmosProcessMessageCache(
                ConnectionString, DatabaseId, ContainerId, jsonProcessor, typeResolver);
            await sut.Collect(message, cancellationToken: CancellationToken.None);

            // Act
            IEnumerable<Message> actual = await sut.Query(
                operationId: message.TracingProperties.OperationId,
                cancellationToken: CancellationToken.None);

            // Assert
            actual.Should().ContainSingle();
            actual.Single().Should().BeEquivalentTo(message);
        }

        [TestMethod, AutoData]
        public async Task item_sets_ttl_correctly(
            Message message,
            IJsonProcessor jsonProcessor,
            TypeResolver typeResolver)
        {
            // Arrange
            var sut = new CosmosProcessMessageCache(
                ConnectionString, DatabaseId, ContainerId, jsonProcessor, typeResolver);

            // Act
            await sut.Collect(message, CancellationToken.None);

            // Assert
            string id = message.Id;
            var partitionKey = new PartitionKey(id);
            ItemResponse<dynamic> response = await Container.ReadItemAsync<dynamic>(id, partitionKey);
            dynamic actual = response.Resource;
            int ttl = actual.ttl;
            int tenMinutesInSeconds = 60 * 10;
            ttl.Should().Be(tenMinutesInSeconds);
        }
    }
}
