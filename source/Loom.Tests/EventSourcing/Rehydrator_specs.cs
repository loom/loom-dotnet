using System;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Loom.EventSourcing.InMemory;
using Loom.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Loom.EventSourcing
{
    [TestClass]
    public class Rehydrator_specs
    {
        [TestMethod]
        public void sut_is_abstract()
        {
            typeof(Rehydrator<>).Should().BeAbstract();
        }

        public class Sut :
            Rehydrator<State1>,
            IEventHandler<State1, Event1>,
            IEventHandler<State1, Event2>
        {
            private readonly Func<State1, Event1, State1> _handler1;
            private readonly Func<State1, Event2, State1> _handler2;

            public Sut(
                Func<string, State1> seedFactory,
                IEventReader eventReader,
                Func<State1, Event1, State1> handler1 = null,
                Func<State1, Event2, State1> handler2 = null)
                : base(seedFactory, eventReader)
            {
                _handler1 = handler1 ?? ((state, pastEvent) => state);
                _handler2 = handler2 ?? ((state, pastEvent) => state);
            }

            public State1 HandleEvent(State1 state, Event1 pastEvent)
                => _handler1.Invoke(state, pastEvent);

            public State1 HandleEvent(State1 state, Event2 pastEvent)
                => _handler2.Invoke(state, pastEvent);
        }

        [TestMethod, AutoData]
        public async Task sut_correctly_restores_state(
            string streamId,
            Event1 event1,
            Event2 event2,
            InMemoryEventStore<State1> eventStore)
        {
            // Arrange
            object[] events = new object[] { event1, event2 };
            await eventStore.CollectEvents(streamId, startVersion: 1, events);

            Sut sut = new(
                seedFactory: x => new(Hash(x)),
                eventStore,
                handler1: (s, e) => new State1(s.Value + e.Value),
                handler2: (s, e) => new State1(s.Value + e.Value));

            // Act
            Snapshot<State1> actual = await sut.RehydrateState(streamId);

            // Assert
            int expected = Hash(streamId) + event1.Value + event2.Value;
            actual.State.Value.Should().Be(expected);
        }

        [TestMethod]
        [InlineAutoData(2)]
        [InlineAutoData(3)]
        [InlineAutoData(4)]
        public async Task sut_correctly_returns_version(
            int count,
            string streamId,
            Generator<Event1> generator,
            InMemoryEventStore<State1> eventStore)
        {
            // Arrange
            Event1[] events = generator.Take(count).ToArray();
            await eventStore.CollectEvents(streamId, startVersion: 1, events);

            Sut sut = new(
                seedFactory: x => new(Hash(x)),
                eventStore,
                handler1: (s, e) => new State1(s.Value + e.Value),
                handler2: (s, e) => new State1(s.Value + e.Value));

            // Act
            Snapshot<State1> actual = await sut.RehydrateState(streamId);

            // Assert
            actual.Version.Should().Be(count);
        }

        [TestMethod, AutoData]
        public async Task sut_correctly_sets_stream_id(
            string streamId,
            Event1 event1,
            Event2 event2,
            InMemoryEventStore<State1> eventStore)
        {
            object[] events = new object[] { event1, event2 };
            await eventStore.CollectEvents(streamId, startVersion: 1, events);
            Sut sut = new(seedFactory: x => new(Hash(x)), eventStore);

            Snapshot<State1> actual = await sut.RehydrateState(streamId);

            actual.StreamId.Should().Be(streamId);
        }

        private static int Hash(object x) => x.GetHashCode();
    }
}
