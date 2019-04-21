namespace Loom.EventSourcing
{
    using System;
    using System.Collections.Generic;
    using AutoFixture;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ConventionalEventProducer_specs
    {
        public class State
        {
            public State(int value) => Value = value;

            public int Value { get; }
        }

        public class UnknownCommand
        {
        }

        public class DecreaseValueTwice
        {
        }

        public class ValueChanged
        {
            public ValueChanged(int value) => Value = value;

            public int Value { get; }
        }

        public class EventProducer : ConventionalEventProducer<State>
        {
            private IEnumerable<object> ProduceEvents(
                State state, DecreaseValueTwice command)
            {
                int seed = state.Value;
                yield return new ValueChanged(seed - 1);
                yield return new ValueChanged(seed - 2);
            }
        }

        [TestMethod]
        public void sut_implements_IEventProducerT()
        {
            Type sut = typeof(ConventionalEventProducer<State>);
            sut.Should().Implement<IEventProducer<State>>();
        }

        [TestMethod]
        public void given_unknown_command_then_ProduceEvents_throws_exception()
        {
            // Arrange
            IEventProducer<State> sut = new EventProducer();
            State state = new Fixture().Create<State>();
            object command = new UnknownCommand();

            // Act
            Action action = () => sut.ProduceEvents(state, command);

            // Assert
            action.Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        public void given_known_command_then_ProduceEvents_returns_events_correctly()
        {
            // Arrange
            IEventProducer<State> sut = new EventProducer();
            State state = new Fixture().Create<State>();
            object command = new DecreaseValueTwice();

            // Act
            IEnumerable<object> actual = sut.ProduceEvents(state, command);

            // Assert
            actual.Should().BeEquivalentTo(
                new ValueChanged(state.Value - 1),
                new ValueChanged(state.Value - 2));
        }
    }
}
