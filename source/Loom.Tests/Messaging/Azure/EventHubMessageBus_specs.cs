namespace Loom.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using AutoFixture;
    using FluentAssertions;
    using Loom.Json;
    using Loom.Testing;
    using Microsoft.Azure.EventHubs;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;

    [TestClass]
    public class EventHubMessageBus_specs
    {
        public static EventHubClient EventHub { get; set; }

        public static EventHubRuntimeInformation EventHubInformation { get; set; }

        public static JsonProcessor JsonProcessor { get; set; }

        public static TypeResolver TypeResolver { get; set; }

        public static EventHubMessageBus Sut { get; set; }

        public PartitionReceiver[] Receivers { get; set; }

        [ClassInitialize]
        public static async Task ClassInitialize(TestContext context)
        {
            if (context.Properties.TryGetValue("EventHubNamespaceConnectionString", out object connectionStringValue) &&
                connectionStringValue is string connectionString &&
                context.Properties.TryGetValue("EventHubName", out object eventHubNameValue) &&
                eventHubNameValue is string eventHubName)
            {
                var connectionStringBuilder = new EventHubsConnectionStringBuilder(connectionString) { EntityPath = eventHubName };
                EventHub = EventHubClient.CreateFromConnectionString(connectionStringBuilder.ToString());
                EventHubInformation = await EventHub.GetRuntimeInformationAsync();
                JsonProcessor = new JsonProcessor(new JsonSerializer());
                TypeResolver = new TypeResolver(new FullNameTypeNameResolvingStrategy(), new TypeResolvingStrategy());
                Sut = new EventHubMessageBus(EventHub, JsonProcessor, TypeResolver);
            }
            else
            {
                Assert.Inconclusive(@"In order to run tests for EventHubMessageBus class set connection properties via runsettings file.

<?xml version=""1.0"" encoding=""utf-16""?>
<RunSettings>
  <TestRunParameters>
    <Parameter name=""EventHubNamespaceConnectionString"" value=""connection string for your event hub namespace"" />
    <Parameter name=""EventHubName"" value=""your event hub name"" />
    <Parameter name=""MaxMessageSize"" value="" 262144 for Basic pricing tier and 1048576 otherwise"" />
  </TestRunParameters>
</RunSettings>");
            }
        }

        [TestInitialize]
        public async Task TestInitialize()
        {
            string consumerGroupName = "$Default";
            Task<PartitionReceiver> Create(string partitionId) => CreateReceiver(consumerGroupName, partitionId);
            Receivers = await Task.WhenAll(EventHubInformation.PartitionIds.Select(Create));
        }

        private static async Task<PartitionReceiver> CreateReceiver(string consumerGroup, string partitionId)
        {
            EventHubPartitionRuntimeInformation partition = await EventHub.GetPartitionRuntimeInformationAsync(partitionId);
            var position = EventPosition.FromSequenceNumber(partition.LastEnqueuedSequenceNumber, inclusive: false);
            return EventHub.CreateReceiver(consumerGroup, partitionId, position);
        }

        [TestCleanup]
        public async Task TestCleanup()
        {
            await Task.WhenAll(Receivers.Select(r => r.CloseAsync()));
        }

        private async Task<EventData[]> ReceiveEvents(int maxCountPerPartition)
        {
            Task<EventData[]> Receive(PartitionReceiver receiver) => ReceiveEvents(receiver, maxCountPerPartition);
            IEnumerable<EventData>[] results = await Task.WhenAll(Receivers.Select(Receive));
            return results.SelectMany(_ => _).ToArray();
        }

        private static async Task<EventData[]> ReceiveEvents(
            PartitionReceiver receiver, int maxCountPerPartition)
        {
            var events = new List<EventData>();

            while (true)
            {
                IEnumerable<EventData> result = await receiver.ReceiveAsync(maxCountPerPartition);
                if (result == null)
                {
                    break;
                }

                events.AddRange(result);

                if (events.Count >= maxCountPerPartition)
                {
                    break;
                }
            }

            return events.ToArray();
        }

        [TestMethod, AutoData]
        public async Task Send_sends_single_message(
            string id,
            MessageData1 data,
            TracingProperties tracingProperties,
            string partitionKey)
        {
            var message = new Message(id, data, tracingProperties);

            await Sut.Send(new[] { message }, partitionKey);

            EventData[] events = await ReceiveEvents(maxCountPerPartition: 1);
            events.Should().ContainSingle();
        }

        [TestMethod, AutoData]
        public async Task Send_serializes_data_correctly(
            string id,
            MessageData1 data,
            TracingProperties tracingProperties,
            string partitionKey)
        {
            var message = new Message(id, data, tracingProperties);

            await Sut.Send(new[] { message }, partitionKey);

            EventData[] events = await ReceiveEvents(maxCountPerPartition: 1);
            string content = Encoding.UTF8.GetString(events[0].Body.Array);
            MessageData1 actual = JsonProcessor.FromJson<MessageData1>(content);
            actual.Should().BeEquivalentTo(data);
        }

        [TestMethod, AutoData]
        public async Task Send_sets_properties_correctly(
            string id,
            MessageData1 data,
            TracingProperties tracingProperties,
            string partitionKey)
        {
            var message = new Message(id, data, tracingProperties);

            await Sut.Send(new[] { message }, partitionKey);

            EventData[] events = await ReceiveEvents(maxCountPerPartition: 1);
            IDictionary<string, object> properties = events[0].Properties;
            properties.Should().Contain("Id", id);
            properties.Should().Contain("Type", TypeResolver.ResolveTypeName<MessageData1>());
            properties.Should().Contain("OperationId", tracingProperties.OperationId);
            properties.Should().Contain("Contributor", tracingProperties.Contributor);
            properties.Should().Contain("ParentId", tracingProperties.ParentId);
        }

        [TestMethod, AutoData]
        public async Task Send_sets_partition_key_correctly(
            Message message, string partitionKey)
        {
            await Sut.Send(new[] { message }, partitionKey);

            EventData[] events = await ReceiveEvents(maxCountPerPartition: 1);
            string actual = events[0].SystemProperties.PartitionKey;
            actual.Should().Be(partitionKey);
        }

        [TestMethod, AutoData]
        public async Task Send_sends_massive_messages_correctly(
            Generator<char> generator, string partitionKey)
        {
            int count = 10000;
            Message[] messages = Enumerable
                .Range(0, count)
                .Select(_ => generator.First())
                .Select(c => new string(c, 1000))
                .Select(s => new MessageData1(1, s))
                .Select(d => new Message($"{Guid.NewGuid()}", d, tracingProperties: default))
                .ToArray();

            await Sut.Send(messages, partitionKey);

            EventData[] events = await ReceiveEvents(maxCountPerPartition: count);
            events.Should().HaveCount(count);
            events
                .Select(e => e.Body.Array)
                .Select(Encoding.UTF8.GetString)
                .Select(JsonProcessor.FromJson<MessageData1>)
                .Should()
                .BeEquivalentTo(messages.Select(x => x.Data), c => c.WithStrictOrdering());
        }
    }
}
