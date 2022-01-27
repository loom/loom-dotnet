using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Producer;
using FluentAssertions;
using Loom.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Loom.Messaging.Azure
{
    [TestClass]
    public class EventHubMessageBus_specs
    {
        public static string? ConnectionString { get; set; }

        public static string? EventHubName { get; set; }

        public EventHubProducerClient? Producer { get; set; }

        public EventHubConsumerClient? Consumer { get; set; }

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            if (context.Properties.TryGetValue("EventHubNamespaceConnectionString", out object? connectionStringValue) &&
                connectionStringValue is string connectionString &&
                context.Properties.TryGetValue("EventHubName", out object? eventHubNameValue) &&
                eventHubNameValue is string eventHubName)
            {
                ConnectionString = connectionString;
                EventHubName = eventHubName;
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
        public void TestInitialize()
        {
            Producer = new(ConnectionString, EventHubName);
            Consumer = new(consumerGroup: "$Default", ConnectionString, EventHubName);
        }

        [TestCleanup]
        public async Task TestCleanup()
        {
            await Producer!.CloseAsync();
            await Consumer!.CloseAsync();
        }

        private async Task<EventData[]> ReceiveEvents(TimeSpan maximumWaitTime)
        {
            List<EventData> events = new();
            var readOptions = new ReadEventOptions { MaximumWaitTime = maximumWaitTime };
            await foreach (PartitionEvent partitionEvent in
                Consumer!.ReadEventsAsync(startReadingAtEarliestEvent: false, readOptions))
            {
                if (partitionEvent.Data == null)
                {
                    break;
                }

                events.Add(partitionEvent.Data);
            }

            return events.ToArray();
        }

        [TestMethod, AutoData]
        public async Task Send_sends_single_message_correctly(
            IEventConverter converter,
            string id,
            string processId,
            string initiator,
            string predecessorId,
            MessageData1 data,
            string partitionKey)
        {
            var sut = new EventHubMessageBus(Producer!, converter);
            Message message = new(id, processId, initiator, predecessorId, data);
            Task<EventData[]> receiveTask = ReceiveEvents(maximumWaitTime: TimeSpan.FromSeconds(1));

            await sut.Send(new[] { message }, partitionKey);

            EventData[] events = await receiveTask;
            events.Should().ContainSingle();
            Message? actual = converter.TryConvertToMessage(events[0]);
            actual.Should().BeEquivalentTo(message);
        }

        [TestMethod, AutoData]
        public async Task Send_sets_partition_key_correctly(
            IEventConverter converter, Message message, string partitionKey)
        {
            var sut = new EventHubMessageBus(Producer!, converter);
            Task<EventData[]> receiveTask = ReceiveEvents(maximumWaitTime: TimeSpan.FromSeconds(1));

            await sut.Send(new[] { message }, partitionKey);

            EventData[] events = await receiveTask;
            string actual = events[0].PartitionKey;
            actual.Should().Be(partitionKey);
        }

        [TestMethod, AutoData]
        public async Task Send_sends_massive_messages_correctly(
            IEventConverter converter,
            Generator<char> generator,
            string processId,
            string initiator,
            string predecessorId,
            string partitionKey)
        {
            var sut = new EventHubMessageBus(Producer!, converter);
            int count = 1000;
            Message[] messages = Enumerable
                .Range(0, count)
                .Select(_ => new string(generator.First(), 1000))
                .Select(value => new MessageData1(1, value))
                .Select(data => new Message(Id: $"{Guid.NewGuid()}", processId, initiator, predecessorId, data))
                .ToArray();
            Task<EventData[]> receiveTask = ReceiveEvents(maximumWaitTime: TimeSpan.FromSeconds(3));

            await sut.Send(messages, partitionKey);

            EventData[] events = await receiveTask;
            events.Length.Should().Be(count);
            IEnumerable<Message?> actual = events.Select(converter.TryConvertToMessage).ToArray();
            actual.Should().BeEquivalentTo(
                messages,
                opts =>
                opts.Excluding(m => m.ProcessId)
                    .Excluding(m => m.Initiator)
                    .Excluding(m => m.PredecessorId)
                    .WithStrictOrdering());
        }

        [TestMethod, AutoData]
        public async Task even_if_no_message_then_Send_does_not_fail(
            IEventConverter converter, string partitionKey)
        {
            var sut = new EventHubMessageBus(Producer!, converter);
            Func<Task> action = () => sut.Send(Array.Empty<Message>(), partitionKey);
            await action.Should().NotThrowAsync();
        }
    }
}
