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

    [TestClass]
    public class TableEventStore_specs :
        EventStoreUnitTests<TableEventStore<State1>>
    {
        private CloudTable Table { get; } = StorageEmulator.EventStoreTable;

        protected override TableEventStore<State1> GenerateEventStore(IMessageBus eventBus)
        {
            return GenerateEventStore<State1>(eventBus);
        }

        private TableEventStore<T> GenerateEventStore<T>(IMessageBus eventBus)
        {
            return new TableEventStore<T>(Table, TypeResolver, Serializer, eventBus);
        }

        [TestMethod, AutoData]
        public async Task sut_supports_multiple_state_types_having_same_stream_id(
            IMessageBus eventBus, Guid streamId, Event1 evt1, Event2 evt2)
        {
            // Arrange
            TableEventStore<State1> store1 = GenerateEventStore<State1>(eventBus);
            TableEventStore<State2> store2 = GenerateEventStore<State2>(eventBus);

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
            TableEventStore<State1> sut = GenerateEventStore(eventBus);
            Func<Task> action = () => sut.CollectEvents(streamId, startVersion: 1, Array.Empty<object>());
            await action.Should().NotThrowAsync();
        }
    }
}
