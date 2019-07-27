namespace Loom.EventSourcing
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Threading.Tasks;
    using AutoFixture;
    using FluentAssertions;
    using Loom.Testing;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class StateRehydrator_specs
    {
        public class State : IVersioned
        {
            public State() => (Version, Value) = (default, default);

            public State(long version, int value)
                => (Version, Value) = (version, value);

            public long Version { get; }

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
            {
                return new State(
                    state.Version + 1,
                    state.Value + valueAdded.Amount);
            }
        }

        [TestMethod]
        public void sut_implements_IStateRehydratorT()
        {
            Type sut = typeof(StateRehydrator<State>);
            sut.Should().Implement<IStateRehydrator<State>>();
        }

        [TestMethod, AutoData]
        public async Task given_no_event_then_with_modest_constructor_TryRehydrateState_returns_null(
            EventHandler handler, Guid streamId)
        {
            // Arrange
            IEventReader eventReader =
                new DelegatingEventReader(
                    (stream, from) =>
                    Task.FromResult(Enumerable.Empty<object>()));

            IEventHandler<State> eventHandler = new EventHandlerDelegate<State>(handler);

            var sut = new StateRehydrator<State>(eventReader, eventHandler);

            // Act
            State actual = await sut.TryRehydrateState(streamId);

            // Assert
            actual.Should().BeNull();
        }

        [TestMethod, AutoData]
        public async Task given_some_events_then_with_modest_constructor_TryRehydrateState_restores_state_correctly(
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

            var sut = new StateRehydrator<State>(eventReader, eventHandler);

            State expected = eventHandler.HandleEvents(new State(), events);

            // Act
            State actual = await sut.TryRehydrateState(streamId);

            // Assert
            actual.Should().BeEquivalentTo(expected);
        }

        [TestMethod, AutoData]
        public async Task given_no_event_then_TryRehydrateStateAt_returns_null(
            Guid streamId, long version, EventHandler handler)
        {
            // Arrange
            IEventReader eventReader =
                new DelegatingEventReader(
                    (stream, from) =>
                    Task.FromResult(Enumerable.Empty<object>()));

            IEventHandler<State> eventHandler = new EventHandlerDelegate<State>(handler);

            var sut = new StateRehydrator<State>(eventReader, eventHandler);

            // Act
            State actual = await sut.TryRehydrateStateAt(streamId, version);

            // Assert
            actual.Should().BeNull();
        }

        [TestMethod, AutoDataRepeat(10)]
        public async Task given_some_events_then_TryRehydrateStateAt_with_existing_version_restores_state_correctly(
            Generator<ValueAdded> generator,
            Guid streamId,
            [Range(1, 10)] long version,
            EventHandler handler)
        {
            // Arrange
            var events = new List<object>(generator.Where(x => x.Amount >= 0).Take(10));

            IEventReader eventReader =
                new DelegatingEventReader(
                    (stream, from) => stream == streamId
                    ? Task.FromResult(events.Skip((int)from - 1))
                    : Task.FromResult(Enumerable.Empty<object>()));

            IEventHandler<State> eventHandler = new EventHandlerDelegate<State>(handler);

            var sut = new StateRehydrator<State>(eventReader, eventHandler);

            State expected = eventHandler.HandleEvents(new State(), events.Take((int)version));

            // Act
            State actual = await sut.TryRehydrateStateAt(streamId, version);

            // Assert
            actual.Should().BeEquivalentTo(expected);
        }

        [TestMethod, AutoDataRepeat(10)]
        public async Task given_some_events_then_TryRehydrateStateAt_with_nonexistent_version_throws_exception(
            Generator<ValueAdded> generator,
            Guid streamId,
            [Range(11, 20)] long version,
            EventHandler handler)
        {
            // Arrange
            var events = new List<object>(generator.Where(x => x.Amount >= 0).Take(10));

            IEventReader eventReader =
                new DelegatingEventReader(
                    (stream, from) => stream == streamId
                    ? Task.FromResult(events.Skip((int)from - 1))
                    : Task.FromResult(Enumerable.Empty<object>()));

            IEventHandler<State> eventHandler = new EventHandlerDelegate<State>(handler);

            var sut = new StateRehydrator<State>(eventReader, eventHandler);

            // Act
            Func<Task> action = () => sut.TryRehydrateStateAt(streamId, version);

            // Assert
            await action.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}
