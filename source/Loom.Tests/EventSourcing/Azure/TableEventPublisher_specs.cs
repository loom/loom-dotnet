namespace Loom.EventSourcing.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Threading.Tasks;
    using AutoFixture;
    using FluentAssertions;
    using Loom.Messaging;
    using Loom.Testing;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TableEventPublisher_specs
    {
        private CloudTable Table { get; set; }

        private TypeResolver TypeResolver { get; } = new TypeResolver(
            new FullNameTypeNameResolvingStrategy(),
            new FullNameTypeResolvingStrategy());

        [TestInitialize]
        public async Task TestInitialize()
        {
            CloudTable table = CloudStorageAccount
                .DevelopmentStorageAccount
                .CreateCloudTableClient()
                .GetTableReference("PublisherTestingEventStore");

            await table.DeleteIfExistsAsync();
            await table.CreateAsync();

            Table = table;
        }

        [TestMethod, AutoDataRepeat(10)]
        public async Task sut_publishes_cold_pending_events(
            Guid streamId, int startVersion, IEnumerable<Event1> events, TracingProperties tracingProperties)
        {
            // Arrange
            var eventBus = new MessageBusDouble(errors: 1);
            var eventStore = new TableEventStore<State1>(Table, TypeResolver, eventBus);
            try
            {
                await eventStore.CollectEvents(streamId, startVersion, events, tracingProperties);
            }
            catch
            {
            }

            List<(ImmutableArray<Message>, string)> expected = eventBus.Calls.ToList();
            eventBus.Clear();

            TimeSpan minimumPendingTime = TimeSpan.Zero;
            var sut = new TableEventPublisher(Table, TypeResolver, eventBus, minimumPendingTime);

            // Act
            await sut.PublishPendingEvents();

            // Assert
            eventBus.Calls.Should().BeEquivalentTo(expected);
        }

        [TestMethod, AutoData]
        public async Task sut_publishes_cold_pending_events_sequentially(
            Guid streamId, [Range(2, 10)] int transactions, IFixture builder)
        {
            // Arrange
            var eventBus = new MessageBusDouble(errors: transactions);
            var eventStore = new TableEventStore<State1>(Table, TypeResolver, eventBus);
            int startVersion = 1;
            for (int i = 0; i < transactions; i++)
            {
                IEnumerable<Event1> events = builder.CreateMany<Event1>();
                TracingProperties tracingProperties = builder.Create<TracingProperties>();
                try
                {
                    await eventStore.CollectEvents(streamId, startVersion, events, tracingProperties);
                }
                catch
                {
                }

                startVersion += events.Count();
            }

            eventBus.Clear();

            TimeSpan minimumPendingTime = TimeSpan.Zero;
            var sut = new TableEventPublisher(Table, TypeResolver, eventBus, minimumPendingTime);

            // Act
            await sut.PublishPendingEvents();

            // Assert
            eventBus.Calls
                    .SelectMany(x => x.messages)
                    .Select(x => (dynamic)x.Data)
                    .Select(x => (long)x.Version)
                    .Should()
                    .BeInAscendingOrder();
        }

        [TestMethod, AutoDataRepeat(10)]
        public async Task sut_does_not_publish_hot_pending_events(
            Guid streamId, int startVersion, IEnumerable<Event1> events, TracingProperties tracingProperties)
        {
            // Arrange
            var eventBus = new MessageBusDouble(errors: 1);
            var eventStore = new TableEventStore<State1>(Table, TypeResolver, eventBus);
            try
            {
                await eventStore.CollectEvents(streamId, startVersion, events, tracingProperties);
            }
            catch
            {
            }

            eventBus.Clear();

            var minimumPendingTime = TimeSpan.FromMilliseconds(1000);
            var sut = new TableEventPublisher(Table, TypeResolver, eventBus, minimumPendingTime);

            // Act
            await sut.PublishPendingEvents();

            // Assert
            eventBus.Calls.Should().BeEmpty();
        }
    }
}
