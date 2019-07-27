namespace Loom.EventSourcing
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Threading.Tasks;
    using AutoFixture;
    using AutoFixture.Kernel;
    using FluentAssertions;
    using Loom.Testing;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SnapshottedStateRehydrator_specs
    {
        public class State : IVersioned
        {
            public State(Guid id) => Id = id;

            public State(Guid id, long version, int value)
                => (Id, Version, Value) = (id, version, value);

            public Guid Id { get; }

            public long Version { get; }

            public int Value { get; }

            public static State SeedFactory(Guid id) => new State(id);
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
                    state.Id,
                    state.Version + 1,
                    state.Value + valueAdded.Amount);
            }
        }

        [TestMethod]
        public void sut_implements_IStateRehydratorT()
        {
            Type sut = typeof(SnapshottedStateRehydrator<State>);
            sut.Should().Implement<IStateRehydrator<State>>();
        }

        [TestMethod, AutoData]
        public async Task given_no_snapshot_and_no_event_then_TryRehydrateState_returns_null(
            EventHandler handler, Guid streamId)
        {
            // Arrange
            ISnapshotReader<State> snapshotReader =
                new DelegatingSnapshotReader<State>(
                    stream => Task.FromResult<State>(default));

            IEventReader eventReader =
                new DelegatingEventReader(
                    (stream, from) =>
                    Task.FromResult(Enumerable.Empty<object>()));

            IEventHandler<State> eventHandler = new EventHandlerDelegate<State>(handler);

            var sut = new SnapshottedStateRehydrator<State>(
                State.SeedFactory, snapshotReader, eventReader, eventHandler);

            // Act
            State actual = await sut.TryRehydrateState(streamId);

            // Assert
            actual.Should().BeNull();
        }

        [TestMethod, AutoData]
        public async Task given_no_snapshot_and_some_events_then_TryRehydrateState_restores_state_correctly(
            Generator<ValueAdded> generator, Guid streamId, EventHandler handler)
        {
            // Arrange
            ISnapshotReader<State> snapshotReader =
                new DelegatingSnapshotReader<State>(
                    stream => Task.FromResult<State>(default));

            var events = new List<object>(generator.Where(x => x.Amount >= 0).Take(10));

            IEventReader eventReader =
                new DelegatingEventReader(
                    (stream, from) => stream == streamId
                    ? Task.FromResult(events.Skip((int)from - 1))
                    : Task.FromResult(Enumerable.Empty<object>()));

            IEventHandler<State> eventHandler = new EventHandlerDelegate<State>(handler);

            var sut = new SnapshottedStateRehydrator<State>(
                State.SeedFactory, snapshotReader, eventReader, eventHandler);

            State expected = eventHandler.HandleEvents(new State(streamId), events);

            // Act
            State actual = await sut.TryRehydrateState(streamId);

            // Assert
            actual.Should().BeEquivalentTo(expected);
        }

        [TestMethod, AutoData]
        public async Task given_snapshot_and_no_event_then_TryRehydrateState_returns_snapshot_directly(
            IFixture builder, EventHandler handler)
        {
            // Arrange
            var methodQuery = new GreedyConstructorQuery();
            builder.Customizations.Add(new MethodInvoker(methodQuery));
            State snapshot = builder.Create<State>();

            Guid streamId = snapshot.Id;

            ISnapshotReader<State> snapshotReader =
                new DelegatingSnapshotReader<State>(
                    stream => stream == streamId
                    ? Task.FromResult(snapshot)
                    : Task.FromResult<State>(default));

            IEventReader eventReader =
                new DelegatingEventReader(
                    (stream, from) =>
                    Task.FromResult(Enumerable.Empty<object>()));

            IEventHandler<State> eventHandler = new EventHandlerDelegate<State>(handler);

            var sut = new SnapshottedStateRehydrator<State>(
                State.SeedFactory, snapshotReader, eventReader, eventHandler);

            // Act
            State actual = await sut.TryRehydrateState(streamId);

            // Assert
            actual.Should().BeSameAs(snapshot);
        }

        [TestMethod, AutoData]
        public async Task given_snapshot_and_some_events_then_TryRehydrateState_restores_state_correctly(
            IFixture builder, Generator<ValueAdded> generator, EventHandler handler)
        {
            // Arrange
            var methodQuery = new GreedyConstructorQuery();
            builder.Customizations.Add(new MethodInvoker(methodQuery));
            State snapshot = builder.Create<State>();

            Guid streamId = snapshot.Id;

            ISnapshotReader<State> snapshotReader =
                new DelegatingSnapshotReader<State>(
                    stream => stream == streamId
                    ? Task.FromResult(snapshot)
                    : Task.FromResult<State>(default));

            var events = new List<object>(generator.Where(x => x.Amount >= 0).Take(10));

            IEventReader eventReader =
                new DelegatingEventReader(
                    (stream, from) =>
                    stream == streamId && from == snapshot.Version + 1
                    ? Task.FromResult(events.AsEnumerable())
                    : Task.FromResult(Enumerable.Empty<object>()));

            IEventHandler<State> eventHandler = new EventHandlerDelegate<State>(handler);

            var sut = new SnapshottedStateRehydrator<State>(
                State.SeedFactory, snapshotReader, eventReader, eventHandler);

            State expected = eventHandler.HandleEvents(snapshot, events);

            // Act
            State actual = await sut.TryRehydrateState(streamId);

            // Assert
            actual.Should().BeEquivalentTo(expected);
        }

        [TestMethod, AutoData]
        public async Task given_no_event_then_TryRehydrateStateAt_returns_null(
            Guid streamId, long version, ISnapshotReader<State> snapshotReader, EventHandler handler)
        {
            // Arrange
            IEventReader eventReader =
                new DelegatingEventReader(
                    (stream, from) =>
                    Task.FromResult(Enumerable.Empty<object>()));

            IEventHandler<State> eventHandler = new EventHandlerDelegate<State>(handler);

            var sut = new SnapshottedStateRehydrator<State>(
                State.SeedFactory, snapshotReader, eventReader, eventHandler);

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
            ISnapshotReader<State> snapshotReader,
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

            var sut = new SnapshottedStateRehydrator<State>(
                State.SeedFactory, snapshotReader, eventReader, eventHandler);

            State expected = eventHandler.HandleEvents(new State(streamId), events.Take((int)version));

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
            ISnapshotReader<State> snapshotReader,
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

            var sut = new SnapshottedStateRehydrator<State>(
                State.SeedFactory, snapshotReader, eventReader, eventHandler);

            // Act
            Func<Task> action = () => sut.TryRehydrateStateAt(streamId, version);

            // Assert
            await action.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}
