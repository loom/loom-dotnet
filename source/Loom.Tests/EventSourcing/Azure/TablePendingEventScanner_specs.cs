namespace Loom.EventSourcing.Azure
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Loom.Json;
    using Loom.Messaging;
    using Loom.Testing;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;

    [TestClass]
    public class TablePendingEventScanner_specs
    {
        private CloudTable Table { get; set; }

        private TypeResolver TypeResolver { get; } = new TypeResolver(
            new FullNameTypeNameResolvingStrategy(),
            new TypeResolvingStrategy());

        private IJsonProcessor JsonProcessor { get; } = new JsonProcessor(new JsonSerializer());

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
            typeof(TablePendingEventScanner).Should().Implement<IPendingEventScanner>();
        }

        private TableEventStore<State1> GenerateEventStore(IMessageBus eventBus) =>
            new TableEventStore<State1>(Table, TypeResolver, JsonProcessor, eventBus);

        [TestMethod, AutoData]
        public async Task sut_sends_flush_commands_for_streams_containing_cold_pending_events(
            Guid streamId, long startVersion, Event1[] events, MessageBusDouble commandBus)
        {
            // Arrange
            var eventBus = new MessageBusDouble(errors: 1);
            TableEventStore<State1> eventStore = GenerateEventStore(eventBus);
            await TryCatchIgnore(() => eventStore.CollectEvents(streamId, startVersion, events));

            var sut = new TablePendingEventScanner(Table, commandBus);

            // Act
            await sut.ScanPendingEvents();

            // Assert
            commandBus.Calls.Should().ContainSingle();
            (ImmutableArray<Message> messages, string partitionKey) = commandBus.Calls.Single();

            messages.Should().ContainSingle();
            Message message = messages.Single();

            Guid.TryParse(message.Id, out Guid id).Should().BeTrue();
            id.Should().NotBeEmpty();

            message.Data.Should().BeOfType<FlushEvents>();
            message.Data.Should().BeEquivalentTo(new
            {
                StateType = TypeResolver.TryResolveTypeName<State1>(),
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
            TableEventStore<State1> eventStore = GenerateEventStore(eventBus);
            await TryCatchIgnore(() => eventStore.CollectEvents(streamId, startVersion, events));

            var sut = new TablePendingEventScanner(Table, commandBus);

            // Act
            await sut.ScanPendingEvents();

            // Assert
            TracingProperties actual = commandBus.Calls.Single().messages.Single().TracingProperties;
            actual.OperationId.Should().NotBeNullOrWhiteSpace();
            actual.Contributor.Should().Be("Loom.EventSourcing.Azure.TablePendingEventScanner");
            actual.ParentId.Should().BeNull();
        }

        [TestMethod, AutoData]
        public async Task sut_distincts_streams(
            Guid streamId, long startVersion, Event1[] events, MessageBusDouble commandBus)
        {
            // Arrange
            var eventBus = new MessageBusDouble(errors: 2);
            TableEventStore<State1> eventStore = GenerateEventStore(eventBus);
            await TryCatchIgnore(() => eventStore.CollectEvents(streamId, startVersion, events));
            await TryCatchIgnore(() => eventStore.CollectEvents(streamId, startVersion + events.Length, events));

            var sut = new TablePendingEventScanner(Table, commandBus);

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
            TableEventStore<State1> eventStore = GenerateEventStore(eventBus);
            await TryCatchIgnore(() => eventStore.CollectEvents(streamId, startVersion, events));

            var minimumPendingTime = TimeSpan.FromSeconds(1);
            var sut = new TablePendingEventScanner(Table, commandBus, minimumPendingTime);

            // Act
            await sut.ScanPendingEvents();

            // Assert
            commandBus.Calls.Should().BeEmpty();
        }

        private static async Task TryCatchIgnore(Func<Task> action)
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
