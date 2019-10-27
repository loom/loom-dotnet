namespace Loom.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AutoFixture;
    using FluentAssertions;
    using Loom.Testing;
    using Microsoft.Azure.EventHubs;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EventHubMessageBus_specs
    {
        public static EventHubClient EventHub { get; set; }

        public static EventHubRuntimeInformation EventHubInformation { get; set; }

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
            }
            else
            {
                Assert.Inconclusive(@"In order to run tests for EventHubMessageBus class set connection properties via runsettings file.

<?xml version=""1.0"" encoding=""utf-16""?>
<RunSettings>
  <TestRunParameters>
    <Parameter name=""EventHubNamespaceConnectionString"" value=""connection string for your event hub namespace"" />
    <Parameter name=""EventHubName"" value=""your event hub name"" />
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
        public async Task Send_sends_single_message_correctly(
            IEventConverter converter,
            string id,
            MessageData1 data,
            TracingProperties tracingProperties,
            string partitionKey)
        {
            var sut = new EventHubMessageBus(EventHub, converter);
            var message = new Message(id, data, tracingProperties);

            await sut.Send(new[] { message }, partitionKey);

            EventData[] events = await ReceiveEvents(maxCountPerPartition: 1);
            events.Should().ContainSingle();
            Message actual = converter.TryConvertToMessage(events[0]);
            actual.Should().BeEquivalentTo(message);
        }

        [TestMethod, AutoData]
        public async Task Send_sets_partition_key_correctly(
            IEventConverter converter, Message message, string partitionKey)
        {
            var sut = new EventHubMessageBus(EventHub, converter);

            await sut.Send(new[] { message }, partitionKey);

            EventData[] events = await ReceiveEvents(maxCountPerPartition: 1);
            string actual = events[0].SystemProperties.PartitionKey;
            actual.Should().Be(partitionKey);
        }

        [TestMethod, AutoData]
        public async Task Send_sends_massive_messages_correctly(
            IEventConverter converter, Generator<char> generator, string partitionKey)
        {
            var sut = new EventHubMessageBus(EventHub, converter);
            int count = 10000;
            Message[] messages = Enumerable
                .Range(0, count)
                .Select(_ => new string(generator.First(), 1000))
                .Select(value => new MessageData1(1, value))
                .Select(data => new Message($"{Guid.NewGuid()}", data, tracingProperties: default))
                .ToArray();

            await sut.Send(messages, partitionKey);

            EventData[] events = await ReceiveEvents(maxCountPerPartition: count);
            events.Should().HaveCount(count);
            IEnumerable<Message> actual = events.Select(converter.TryConvertToMessage).ToArray();
            actual.Should().BeEquivalentTo(messages, c => c.Excluding(m => m.TracingProperties).WithStrictOrdering());
        }
    }
}
