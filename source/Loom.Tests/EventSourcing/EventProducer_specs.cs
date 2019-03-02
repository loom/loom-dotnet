namespace Loom.EventSourcing
{
    using System;
    using System.Collections.Generic;
    using AutoFixture;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EventProducer_specs
    {
        public class State
        {
            public State(int value) => Value = value;

            public int Value { get; }
        }

        public class UnknownCommandPayload
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

        public class EventProducer : EventProducer<State>
        {
            private IEnumerable<object> ProduceEventPayloads(
                State state, DecreaseValueTwice commandPayload)
            {
                int seed = state.Value;
                yield return new ValueChanged(seed - 1);
                yield return new ValueChanged(seed - 2);
            }
        }

        [TestMethod]
        public void sut_implements_IEventProducerT()
        {
            Type sut = typeof(EventProducer<State>);
            sut.Should().Implement<IEventProducer<State>>();
        }

        [TestMethod]
        public void given_unknown_command_payload_type_then_ProduceEventPayloads_throws_exception()
        {
            // Arrange
            IEventProducer<State> sut = new EventProducer();
            State state = new Fixture().Create<State>();
            object commandPayload = new UnknownCommandPayload();

            // Act
            Action action = () => sut.ProduceEventPayloads(state, commandPayload);

            // Assert
            action.Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        public void given_known_command_payload_then_ProduceEventPayloads_returns_event_payloads_correctly()
        {
            // Arrange
            IEventProducer<State> sut = new EventProducer();
            State state = new Fixture().Create<State>();
            object commandPayload = new DecreaseValueTwice();

            // Act
            IEnumerable<object> actual =
                sut.ProduceEventPayloads(state, commandPayload);

            // Assert
            actual.Should().BeEquivalentTo(
                new ValueChanged(state.Value - 1),
                new ValueChanged(state.Value - 2));
        }
    }
}
