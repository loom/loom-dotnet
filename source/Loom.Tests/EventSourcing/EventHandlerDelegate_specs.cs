using System;
using System.Linq;
using FluentAssertions;
using Loom.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Loom.EventSourcing
{
    [Obsolete("EventHandlerDelegate is deprecated.")]
    [TestClass]
    public class EventHandlerDelegate_specs
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

        public class EventHandler
        {
            public State HandleEvent(State state, Appended appended)
                => new State(state.Value + appended.Value);
        }

        public class UnknownEvent
        {
        }

        [TestMethod]
        public void sut_is_sealed()
        {
            typeof(EventHandlerDelegate<>).Should().BeSealed();
        }

        [TestMethod]
        public void sut_implements_IEventHandlerT()
        {
            Type sut = typeof(EventHandlerDelegate<State>);
            sut.Should().Implement<IEventHandler<State>>();
        }

        [TestMethod, AutoData]
        public void given_unknown_event_then_HandleEvents_throws_exception(
            EventHandler handler, State state)
        {
            var sut = new EventHandlerDelegate<State>(handler);
            Action action = () => sut.HandleEvents(state, new[] { new UnknownEvent() });
            action.Should().Throw<InvalidOperationException>();
        }

        [TestMethod, AutoData]
        public void given_known_event_then_HandleEvents_invokes_handler_correctly(
            EventHandler handler, string seed, Appended evt)
        {
            var sut = new EventHandlerDelegate<State>(handler);
            var state = new State(seed);
            Appended[] events = new[] { evt };

            State actual = sut.HandleEvents(state, events);

            string expected = events.Aggregate(seed, (v, e) => v + e.Value);
            actual.Value.Should().Be(expected);
        }
    }
}
