namespace Loom.EventSourcing.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
            return new TableEventStore<T>(Table, TypeResolver, JsonProcessor, eventBus);
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

        [TestMethod, AutoData]
        public async Task QueryEventMessages_correctly_restores_event_messages(
            Guid streamId,
            Event1 evt1,
            Event2 evt2,
            Event3 evt3,
            IMessageBus eventBus,
            TracingProperties tracingProperties)
        {
            // Arrange
            TableEventStore<State1> sut = GenerateEventStore(eventBus);
            object[] events = new object[] { evt1, evt2, evt3 };
            DateTime nowUtc = DateTime.UtcNow;
            await sut.CollectEvents(streamId, startVersion: 1, events, tracingProperties);

            // Act
            IEnumerable<Message> actual = await sut.QueryEventMessages(streamId);

            // Assert
            actual.Should().HaveSameCount(events);

            actual.Cast<dynamic>()
                  .Select(x => (Guid)x.Data.StreamId)
                  .Should().OnlyContain(x => x == streamId);

            actual.Cast<dynamic>()
                  .Select(x => (long)x.Data.Version)
                  .Should().BeEquivalentTo(new[] { 1, 2, 3 });

            actual.Cast<dynamic>()
                  .Select(x => (object)x.Data.Payload)
                  .Should().BeEquivalentTo(events);

            foreach (DateTime raisedTime in actual.Cast<dynamic>()
                                                  .Select(x => x.Data.RaisedTimeUtc))
            {
                raisedTime.Kind.Should().Be(DateTimeKind.Utc);
                raisedTime.Should().BeCloseTo(nowUtc, precision: 1000);
            }

            actual.ElementAt(0).Data.Should().BeOfType<StreamEvent<Event1>>();
            actual.ElementAt(1).Data.Should().BeOfType<StreamEvent<Event2>>();
            actual.ElementAt(2).Data.Should().BeOfType<StreamEvent<Event3>>();

            actual.Select(x => x.Id).Should().OnlyHaveUniqueItems();
            actual.Should().OnlyContain(x => x.TracingProperties == tracingProperties);
        }

        [TestMethod, AutoData]
        public async Task QueryEventMessages_returns_same_value_for_same_source(
            Guid streamId,
            Event1 evt1,
            Event2 evt2,
            Event3 evt3,
            IMessageBus eventBus,
            TracingProperties tracingProperties)
        {
            TableEventStore<State1> sut = GenerateEventStore(eventBus);
            object[] events = new object[] { evt1, evt2, evt3 };
            await sut.CollectEvents(streamId, startVersion: 1, events, tracingProperties);

            IEnumerable<Message> actual = await sut.QueryEventMessages(streamId);

            actual.Should().BeEquivalentTo(await sut.QueryEventMessages(streamId));
        }
    }
}
