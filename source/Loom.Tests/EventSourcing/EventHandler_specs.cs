namespace Loom.EventSourcing
{
    using System;
    using AutoFixture;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EventHandler_specs
    {
        public class State
        {
            public State(int value) => Value = value;

            public int Value { get; }
        }

        public class EventHandler : EventHandler<State>
        {
            private State Handle(State state, ValueAdded eventPayload)
                => new State(value: state.Value + eventPayload.Value);
        }

        public class UnknownEventPayload
        {
        }

        public class ValueAdded
        {
            public ValueAdded(int value) => Value = value;

            public int Value { get; }
        }

        [TestMethod]
        public void sut_implements_IEventHandlerT()
        {
            Type sut = typeof(EventHandler<State>);
            sut.Should().Implement<IEventHandler<State>>();
        }

        [TestMethod]
        public void given_unknown_event_payload_type_then_Handle_throws_exception()
        {
            // Arrange
            IEventHandler<State> sut = new EventHandler();
            State state = new Fixture().Create<State>();
            object eventPayload = new UnknownEventPayload();

            // Act
            Action action = () => sut.Handle(state, eventPayload);

            // Assert
            action.Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        public void given_known_event_payload_then_Handle_invokes_handler_correctly()
        {
            // Arrange
            IEventHandler<State> sut = new EventHandler();
            var builder = new Fixture();
            State state = builder.Create<State>();
            ValueAdded eventPayload = builder.Create<ValueAdded>();

            // Act
            State actual = sut.Handle(state, eventPayload);

            // Assert
            actual.Value.Should().Be(state.Value + eventPayload.Value);
        }
    }
}
