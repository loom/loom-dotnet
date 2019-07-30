namespace Loom.EventSourcing.Azure
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Loom.Messaging;
    using Loom.Testing;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class PendingTableEventScanner_specs
    {
        private CloudTable Table { get; set; }

        private TypeResolver TypeResolver { get; } = new TypeResolver(
            new FullNameTypeNameResolvingStrategy(),
            new TypeResolvingStrategy());

        [TestInitialize]
        public async Task TestInitialize()
        {
            CloudTable table = CloudStorageAccount
                .DevelopmentStorageAccount
                .CreateCloudTableClient()
                .GetTableReference("DetectorTestingEventStore");

            await table.DeleteIfExistsAsync();
            await table.CreateAsync();

            Table = table;
        }

        [TestMethod]
        public void sut_implements_IPendingEventScanner()
        {
            typeof(PendingTableEventScanner).Should().Implement<IPendingEventScanner>();
        }

        [TestMethod, AutoData]
        public async Task sut_sends_flush_commands_for_streams_containing_cold_pending_events(
            Guid streamId, long startVersion, Event1[] events, MessageBusDouble commandBus)
        {
            // Arrange
            var eventBus = new MessageBusDouble(errors: 1);
            var eventStore = new TableEventStore<State1>(Table, TypeResolver, eventBus);
            await TryForget(() => eventStore.CollectEvents(streamId, startVersion, events));

            var sut = new PendingTableEventScanner(Table, commandBus);

            // Act
            await sut.ScanPendingEvents();

            // Assert
            commandBus.Calls.Should().ContainSingle();
            (ImmutableArray<Message> messages, string partitionKey) = commandBus.Calls.Single();

            messages.Should().ContainSingle();
            Message message = messages.Single();

            Guid.TryParse(message.Id, out Guid id).Should().BeTrue();
            id.Should().NotBeEmpty();

            message.Data.Should().BeOfType<FlushTableEvents>();
            message.Data.Should().BeEquivalentTo(new
            {
                StateType = TypeResolver.ResolveTypeName<State1>(),
                StreamId = streamId,
            });

            partitionKey.Should().Be($"{streamId}");
        }

        [TestMethod, AutoData]
        public async Task sut_sets_tracing_properties_correctly(
            Guid streamId, long startVersion, Event1[] events, MessageBusDouble commandBus)
        {
            // Arrange
            var eventBus = new MessageBusDouble(errors: 1);
            var eventStore = new TableEventStore<State1>(Table, TypeResolver, eventBus);
            await TryForget(() => eventStore.CollectEvents(streamId, startVersion, events));

            var sut = new PendingTableEventScanner(Table, commandBus);

            // Act
            await sut.ScanPendingEvents();

            // Assert
            TracingProperties actual = commandBus.Calls.Single().messages.Single().TracingProperties;
            actual.OperationId.Should().NotBeNullOrWhiteSpace();
            actual.Contributor.Should().Be("Loom.EventSourcing.Azure.PendingTableEventScanner");
            actual.ParentId.Should().BeNull();
        }

        [TestMethod, AutoData]
        public async Task sut_distincts_streams(
            Guid streamId, long startVersion, Event1[] events, MessageBusDouble commandBus)
        {
            // Arrange
            var eventBus = new MessageBusDouble(errors: 2);
            var eventStore = new TableEventStore<State1>(Table, TypeResolver, eventBus);
            await TryForget(() => eventStore.CollectEvents(streamId, startVersion, events));
            await TryForget(() => eventStore.CollectEvents(streamId, startVersion + events.Length, events));

            var sut = new PendingTableEventScanner(Table, commandBus);

            // Act
            await sut.ScanPendingEvents();

            // Assert
            commandBus.Calls.Should().ContainSingle();
        }

        [TestMethod, AutoData]
        public async Task sut_does_not_send_flush_command_for_streams_containing_only_hot_pending_events(
            Guid streamId, long startVersion, Event1[] events, MessageBusDouble commandBus)
        {
            // Arrange
            var eventBus = new MessageBusDouble(errors: 1);
            var eventStore = new TableEventStore<State1>(Table, TypeResolver, eventBus);
            await TryForget(() => eventStore.CollectEvents(streamId, startVersion, events));

            var minimumPendingTime = TimeSpan.FromSeconds(1);
            var sut = new PendingTableEventScanner(Table, commandBus, minimumPendingTime);

            // Act
            await sut.ScanPendingEvents();

            // Assert
            commandBus.Calls.Should().BeEmpty();
        }

        private static async Task TryForget(Func<Task> action)
        {
            try
            {
                await action.Invoke();
            }
            catch
            {
            }
        }
    }
}
