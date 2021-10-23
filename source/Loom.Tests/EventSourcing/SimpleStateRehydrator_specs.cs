using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Loom.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Loom.EventSourcing
{
    [TestClass]
    public class SimpleStateRehydrator_specs
    {
        public class State
        {
            public State(int value = default) => Value = value;

            public int Value { get; }
        }

        public class ValueAdded
        {
            public ValueAdded(int amount) => Amount = amount;

            public int Amount { get; }
        }

        public class EventHandler
        {
            public State HandleEvent(State state, ValueAdded valueAdded)
                => new State(state.Value + valueAdded.Amount);
        }

        [TestMethod, AutoData]
        public async Task given_no_event_then_TryRehydrateState_returns_null(
            EventHandler handler, Guid streamId)
        {
            // Arrange
            IEventReader eventReader =
                new DelegatingEventReader(
                    (stream, from) =>
                    Task.FromResult(Enumerable.Empty<object>()));

            IEventHandler<State> eventHandler = new EventHandlerDelegate<State>(handler);

            var sut = new SimpleStateRehydrator<State>(
                seedFactory: _ => new State(), eventReader, eventHandler);

            // Act
            State actual = await sut.TryRehydrateState(streamId);

            // Assert
            actual.Should().BeNull();
        }

        [TestMethod, AutoData]
        public async Task given_some_events_then_TryRehydrateState_restores_state_correctly(
            Generator<ValueAdded> generator, Guid streamId, EventHandler handler)
        {
            // Arrange
            var events = new List<object>(generator.Where(x => x.Amount >= 0).Take(10));

            IEventReader eventReader =
                new DelegatingEventReader(
                    (stream, from) => stream == streamId
                    ? Task.FromResult(events.Skip((int)from - 1))
                    : Task.FromResult(Enumerable.Empty<object>()));

            IEventHandler<State> eventHandler = new EventHandlerDelegate<State>(handler);

            var sut = new SimpleStateRehydrator<State>(
                seedFactory: _ => new State(), eventReader, eventHandler);

            State expected = eventHandler.HandleEvents(new State(), events);

            // Act
            State actual = await sut.TryRehydrateState(streamId);

            // Assert
            actual.Should().BeEquivalentTo(expected);
        }
    }
}
