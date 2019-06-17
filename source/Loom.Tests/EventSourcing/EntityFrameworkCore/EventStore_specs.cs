namespace Loom.EventSourcing.EntityFrameworkCore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AutoFixture;
    using FluentAssertions;
    using Loom.Messaging;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;

    [TestClass]
    public class EventStore_specs
    {
        public struct ComplexValue
        {
            public ComplexValue(int value1, string value2)
            {
                Value1 = value1;
                Value2 = value2;
            }

            public int Value1 { get; }

            public string Value2 { get; }
        }

        public class Event1
        {
            public Event1(int value1, string value2, ComplexValue value3)
            {
                Value1 = value1;
                Value2 = value2;
                Value3 = value3;
            }

            public int Value1 { get; }

            public string Value2 { get; }

            public ComplexValue Value3 { get; }
        }

        public class Event2
        {
            public Event2(int value1, string value2)
            {
                Value1 = value1;
                Value2 = value2;
            }

            public int Value1 { get; }

            public string Value2 { get; }
        }

        [TestMethod]
        public void sut_implements_IEventCollector()
        {
            typeof(EventStore).Should().Implement<IEventCollector>();
        }

        [TestMethod]
        public async Task given_single_event_then_CollectEvents_inserts_StreamEvent_entity_correctly()
        {
            // Arrange
            DbContextOptions options = new DbContextOptionsBuilder()
                .UseInMemoryDatabase(databaseName: $"{Guid.NewGuid()}")
                .Options;

            var typeResolver = new TypeResolver(
                new FullNameTypeNameResolvingStrategy(),
                new FullNameTypeResolvingStrategy());

            var sut = new EventStore(
                contextFactory: () => new EventStoreContext(options),
                typeResolver);

            var streamId = Guid.NewGuid();
            (Event1 evt, long version) = new Fixture().Create<(Event1, long)>();

            // Act
            await sut.CollectEvents(streamId, startVersion: version, events: new[] { evt });

            // Assert
            var context = new EventStoreContext(options);
            var query = from e in context.StreamEvents
                        where e.StreamId == streamId
                        select new { e.Version, e.EventType, e.Payload };

            var actual = await query.SingleOrDefaultAsync();

            actual.Should().BeEquivalentTo(new
            {
                Version = version,
                EventType = typeResolver.ResolveTypeName<Event1>(),
                Payload = JsonConvert.SerializeObject(evt),
            });
        }

        [TestMethod]
        public async Task given_multiple_events_then_CollectEvents_inserts_StreamEvent_entities_correctly()
        {
            // Arrange
            DbContextOptions options = new DbContextOptionsBuilder()
                .UseInMemoryDatabase(databaseName: $"{Guid.NewGuid()}")
                .Options;

            var typeResolver = new TypeResolver(
                new FullNameTypeNameResolvingStrategy(),
                new FullNameTypeResolvingStrategy());

            var sut = new EventStore(
                contextFactory: () => new EventStoreContext(options),
                typeResolver);

            var streamId = Guid.NewGuid();
            (Event1 evt1, Event2 evt2, long startVersion) =
                new Fixture().Create<(Event1, Event2, long)>();

            // Act
            await sut.CollectEvents(streamId, startVersion, events: new object[] { evt1, evt2 });

            // Assert
            var context = new EventStoreContext(options);
            var query = from e in context.StreamEvents
                        where e.StreamId == streamId
                        orderby e.Version ascending
                        select new { e.Version, e.EventType, e.Payload };

            var actual = await query.ToListAsync();

            actual.Should().BeEquivalentTo(expectations: new object[]
            {
                new
                {
                    Version = startVersion,
                    EventType = typeResolver.ResolveTypeName<Event1>(),
                    Payload = JsonConvert.SerializeObject(evt1),
                },
                new
                {
                    Version = startVersion + 1,
                    EventType = typeResolver.ResolveTypeName<Event2>(),
                    Payload = JsonConvert.SerializeObject(evt2),
                },
            });
        }

        [TestMethod]
        public async Task CollectEvents_sets_RaisedTimeUtc_property_correctly()
        {
            // Arrange
            DbContextOptions options = new DbContextOptionsBuilder()
                .UseInMemoryDatabase(databaseName: $"{Guid.NewGuid()}")
                .Options;

            var sut = new EventStore(
                () => new EventStoreContext(options),
                new TypeResolver(
                    new FullNameTypeNameResolvingStrategy(),
                    new FullNameTypeResolvingStrategy()));

            var streamId = Guid.NewGuid();
            Event1 evt = new Fixture().Create<Event1>();

            DateTime nearby = DateTime.UtcNow;

            // Act
            await sut.CollectEvents(streamId, startVersion: 1, events: new[] { evt });

            // Assert
            var context = new EventStoreContext(options);
            IQueryable<DateTime> query = from e in context.StreamEvents
                                         where e.StreamId == streamId
                                         select e.RaisedTimeUtc;
            DateTime actual = await query.SingleOrDefaultAsync();
            actual.Kind.Should().Be(DateTimeKind.Utc);
            actual.Should().BeCloseTo(nearby, precision: 1000);
        }

        [TestMethod]
        public void sut_implements_IEventReader()
        {
            typeof(EventStore).Should().Implement<IEventReader>();
        }

        [TestMethod]
        public async Task QueryEvents_reads_event_stream_correctly()
        {
            // Arrange
            DbContextOptions options = new DbContextOptionsBuilder()
                .UseInMemoryDatabase(databaseName: $"{Guid.NewGuid()}")
                .Options;

            var sut = new EventStore(
                () => new EventStoreContext(options),
                new TypeResolver(
                    new FullNameTypeNameResolvingStrategy(),
                    new FullNameTypeResolvingStrategy()));

            var streamId = Guid.NewGuid();
            (Event1 evt1, Event2 evt2) = new Fixture().Create<(Event1, Event2)>();
            object[] events = new object[] { evt1, evt2 };

            await sut.CollectEvents(streamId, startVersion: 1, events);

            // Act
            IEnumerable<object> actual = await sut.QueryEvents(streamId, fromVersion: 1);

            // Assert
            actual.Should().BeEquivalentTo(events);
        }

        [TestMethod]
        public async Task QueryEvents_filters_events_by_version()
        {
            // Arrange
            DbContextOptions options = new DbContextOptionsBuilder()
                .UseInMemoryDatabase(databaseName: $"{Guid.NewGuid()}")
                .Options;

            var sut = new EventStore(
                () => new EventStoreContext(options),
                new TypeResolver(
                    new FullNameTypeNameResolvingStrategy(),
                    new FullNameTypeResolvingStrategy()));

            var streamId = Guid.NewGuid();
            (Event1 evt1, Event2 evt2) = new Fixture().Create<(Event1, Event2)>();
            object[] events = new object[] { evt1, evt2 };

            await sut.CollectEvents(streamId, startVersion: 1, events);

            // Act
            IEnumerable<object> actual = await sut.QueryEvents(streamId, fromVersion: 2);

            // Assert
            actual.Should().BeEquivalentTo(evt2);
        }

        [TestMethod]
        public async Task QueryEvents_filters_events_by_stream()
        {
            // Arrange
            DbContextOptions options = new DbContextOptionsBuilder()
                .UseInMemoryDatabase(databaseName: $"{Guid.NewGuid()}")
                .Options;

            var sut = new EventStore(
                () => new EventStoreContext(options),
                new TypeResolver(
                    new FullNameTypeNameResolvingStrategy(),
                    new FullNameTypeResolvingStrategy()));

            (Event1 evt1, Event2 evt2) = new Fixture().Create<(Event1, Event2)>();

            var streamId = Guid.NewGuid();
            await sut.CollectEvents(streamId, startVersion: 1, events: new[] { evt1 });

            var otherStreamId = Guid.NewGuid();
            await sut.CollectEvents(otherStreamId, startVersion: 2, events: new[] { evt2 });

            // Act
            IEnumerable<object> actual = await sut.QueryEvents(streamId, fromVersion: 1);

            // Assert
            actual.Should().BeEquivalentTo(evt1);
        }

        [TestMethod]
        public async Task QueryEvents_sorts_events_by_version()
        {
            // Arrange
            DbContextOptions options = new DbContextOptionsBuilder()
                .UseInMemoryDatabase(databaseName: $"{Guid.NewGuid()}")
                .Options;

            var sut = new EventStore(
                () => new EventStoreContext(options),
                new TypeResolver(
                    new FullNameTypeNameResolvingStrategy(),
                    new FullNameTypeResolvingStrategy()));

            var streamId = Guid.NewGuid();
            (Event1 evt1, Event2 evt2) = new Fixture().Create<(Event1, Event2)>();
            object[] events = new object[] { evt1, evt2 };

            await sut.CollectEvents(streamId, startVersion: 2, events: new[] { evt2 });
            await sut.CollectEvents(streamId, startVersion: 1, events: new[] { evt1 });

            // Act
            IEnumerable<object> actual = await sut.QueryEvents(streamId, fromVersion: 1);

            // Assert
            actual.Should().BeEquivalentTo(events, c => c.WithStrictOrdering());
        }
    }
}
