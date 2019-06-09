namespace Loom.EventSourcing.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AutoFixture;
    using FluentAssertions;
    using Loom.Messaging;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using static System.Guid;
    using static StorageEmulator;

    [TestClass]
    public class TableEventStore_specs
    {
        [TestMethod]
        public void sut_implements_IEventCollector()
        {
            typeof(TableEventStore).Should().Implement<IEventCollector>();
        }

        [TestMethod]
        public void sut_implements_IEventReader()
        {
            typeof(TableEventStore).Should().Implement<IEventReader>();
        }

        public class Event1
        {
            public Event1(int value) => Value = value;

            public int Value { get; }
        }

        public class Event2
        {
            public Event2(double value) => Value = value;

            public double Value { get; }
        }

        public class Event3
        {
            public Event3(string value) => Value = value;

            public string Value { get; }
        }

        public class Event4
        {
            public Event4(Guid value) => Value = value;

            public Guid Value { get; }
        }

        private static TypeResolver TypeResolver =>
            new TypeResolver(
                new FullNameTypeNameResolvingStrategy(),
                new FullNameTypeResolvingStrategy());

        public static IEnumerable<object[]> Events
        {
            get
            {
                var builder = new Fixture();

                yield return new object[]
                {
                    new List<object>(
                        from e in new object[]
                        {
                            builder.Create<Event1>(),
                            builder.Create<Event2>(),
                            builder.Create<Event3>(),
                            builder.Create<Event4>(),
                        }
                        orderby e.GetHashCode()
                        select e),
                };
            }
        }

        [TestMethod]
        [DynamicData(nameof(Events), DynamicDataSourceType.Property)]
        public async Task QueryEvents_restores_events_correctly(
            List<object> events)
        {
            // Arrange
            var sut = new TableEventStore(EventStoreTable, TypeResolver);
            Guid streamId = NewGuid();
            await sut.CollectEvents(operationId: default,
                                    contributor: default,
                                    parentId: default,
                                    streamId,
                                    startVersion: 1,
                                    events);

            // Act
            IEnumerable<object> actual = await
                sut.QueryEvents(streamId, fromVersion: 1);

            // Assert
            actual.Should().BeEquivalentTo(events, c => c.WithStrictOrdering());
        }

        [TestMethod]
        [DynamicData(nameof(Events), DynamicDataSourceType.Property)]
        public async Task QueryEvents_filters_events_by_stream_id(
            List<object> events)
        {
            // Arrange
            var sut = new TableEventStore(EventStoreTable, TypeResolver);

            Guid streamId = NewGuid();
            int startVersion = 1;

            await sut.CollectEvents(operationId: default,
                                    contributor: default,
                                    parentId: default,
                                    streamId,
                                    startVersion,
                                    events);

            await sut.CollectEvents(operationId: default,
                                    contributor: default,
                                    parentId: default,
                                    streamId: NewGuid(),
                                    startVersion,
                                    events);

            // Act
            IEnumerable<object> actual = await
                sut.QueryEvents(streamId, fromVersion: 1);

            // Assert
            actual.Should().BeEquivalentTo(events, c => c.WithStrictOrdering());
        }

        [TestMethod]
        [DynamicData(nameof(Events), DynamicDataSourceType.Property)]
        public async Task QueryEvents_filters_events_by_version(
            List<object> events)
        {
            // Arrange
            var sut = new TableEventStore(EventStoreTable, TypeResolver);
            Guid streamId = NewGuid();
            await sut.CollectEvents(operationId: default,
                                    contributor: default,
                                    parentId: default,
                                    streamId,
                                    startVersion: 1,
                                    events);

            // Act
            IEnumerable<object> actual = await
                sut.QueryEvents(streamId, fromVersion: 2);

            // Assert
            actual.Should().BeEquivalentTo(events.Skip(1), c => c.WithStrictOrdering());
        }

        [TestMethod]
        public async Task CollectEvents_controls_concurrency()
        {
            // Arrange
            var sut = new TableEventStore(EventStoreTable, TypeResolver);
            Guid streamId = NewGuid();
            int version = 1;
            object[] events = new[] { new Event4(NewGuid()) };
            await sut.CollectEvents(operationId: default,
                                    contributor: default,
                                    parentId: default,
                                    streamId,
                                    startVersion: version,
                                    events);

            // Act
            Func<Task> action = () => sut.CollectEvents(operationId: default,
                                                        contributor: default,
                                                        parentId: default,
                                                        streamId,
                                                        startVersion: version,
                                                        events);

            // Assert
            await action.Should().ThrowAsync<StorageException>();
        }
    }
}
