namespace Loom.EventSourcing.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Loom.Messaging;
    using Loom.Testing;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class TableEventStore_specs :
        EventStoreUnitTests<TableEventStore<State1>>
    {
        protected override TableEventStore<State1> GenerateEventStore(
            TypeResolver typeResolver, IMessageBus eventBus)
        {
            CloudTable table = StorageEmulator.EventStoreTable;
            return new TableEventStore<State1>(table, typeResolver, eventBus);
        }

        [TestMethod, AutoData]
        public async Task sut_supports_multiple_state_types_having_same_stream_id(
            IMessageBus eventBus, Guid streamId, Event1 evt1, Event2 evt2)
        {
            // Arrange
            CloudTable table = StorageEmulator.EventStoreTable;

            var typeResolver = new TypeResolver(
                new FullNameTypeNameResolvingStrategy(),
                new TypeResolvingStrategy());

            var store1 = new TableEventStore<State1>(table, typeResolver, eventBus);
            var store2 = new TableEventStore<State2>(table, typeResolver, eventBus);

            int startVersion = 1;

            // Act
            Func<Task> action = async () =>
            {
                await store1.CollectEvents(streamId, startVersion, new[] { evt1 });
                await store2.CollectEvents(streamId, startVersion, new[] { evt2 });
            };

            // Assert
            await action.Should().NotThrowAsync();

            IEnumerable<object> actual1 = await store1.QueryEvents(streamId, fromVersion: 1);
            actual1.Should().BeEquivalentTo(evt1);

            IEnumerable<object> actual2 = await store2.QueryEvents(streamId, fromVersion: 1);
            actual2.Should().BeEquivalentTo(evt2);
        }

        [TestMethod, AutoData]
        public async Task CollectEvents_does_not_fail_for_empty_event_list(
            IMessageBus eventBus, Guid streamId)
        {
            // Arrange
            CloudTable table = StorageEmulator.EventStoreTable;

            var typeResolver = new TypeResolver(
                new FullNameTypeNameResolvingStrategy(),
                new TypeResolvingStrategy());

            var sut = new TableEventStore<State1>(table, typeResolver, eventBus);

            // Act
            Func<Task> action = () => sut.CollectEvents(streamId, startVersion: 1, Array.Empty<object>());

            // Assert
            await action.Should().NotThrowAsync();
        }
    }
}
