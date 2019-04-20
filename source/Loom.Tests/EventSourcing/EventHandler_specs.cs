namespace Loom.EventSourcing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AutoFixture;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EventHandler_specs
    {
        public class State
        {
            public State(string value) => Value = value;

            public string Value { get; }
        }

        public class Appended
        {
            public Appended(string value) => Value = value;

            public string Value { get; }
        }

        public class EventHandler : EventHandler<State>
        {
            private State Handle(State state, Appended appended)
                => new State(state.Value + appended.Value);
        }

        public class UnknownEvent
        {
        }

        [TestMethod]
        public void sut_implements_IEventHandlerT()
        {
            Type sut = typeof(EventHandler<State>);
            sut.Should().Implement<IEventHandler<State>>();
        }

        [TestMethod]
        public void given_unknown_event_then_HandleEvents_throws_exception()
        {
            // Arrange
            IEventHandler<State> sut = new EventHandler();
            State state = new Fixture().Create<State>();
            IEnumerable<object> events = new[] { new UnknownEvent() };

            // Act
            Action action = () => sut.HandleEvents(state, events);

            // Assert
            action.Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        public void given_known_event_then_HandleEvents_invokes_handler_correctly()
        {
            // Arrange
            IEventHandler<State> sut = new EventHandler();
            var builder = new Fixture();
            string seed = builder.Create<string>();
            var state = new State(seed);
            IEnumerable<Appended> events = new[] { builder.Create<Appended>() };

            // Act
            State actual = sut.HandleEvents(state, events);

            // Assert
            string expected = events.Aggregate(seed, (v, e) => v + e.Value);
            actual.Value.Should().Be(expected);
        }
    }
}
