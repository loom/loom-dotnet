using System;
using System.Collections.Generic;
using FluentAssertions;
using Loom.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Loom.EventSourcing
{
    [Obsolete("EventProducerDelegate is deprecated.")]
    [TestClass]
    public class EventProducerDelegate_specs
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

        public class EventProducer
        {
            public IEnumerable<object> ProduceEvents(
                State state, DecreaseValueTwice command)
            {
                int seed = state.Value;
                yield return new ValueChanged(seed - 1);
                yield return new ValueChanged(seed - 2);
            }
        }

        [TestMethod]
        public void sut_is_sealed()
        {
            typeof(EventProducerDelegate<>).Should().BeSealed();
        }

        [TestMethod]
        public void sut_implements_IEventProducerT()
        {
            Type sut = typeof(EventProducerDelegate<State>);
            sut.Should().Implement<IEventProducer<State>>();
        }

        [TestMethod, AutoData]
        public void given_unknown_command_then_ProduceEvents_throws_exception(
            EventProducer producer, State state, UnknownCommand command)
        {
            var sut = new EventProducerDelegate<State>(producer);
            Action action = () => sut.ProduceEvents(state, command);
            action.Should().Throw<InvalidOperationException>();
        }

        [TestMethod, AutoData]
        public void given_known_command_then_ProduceEvents_returns_events_correctly(
            EventProducer producer, State state, DecreaseValueTwice command)
        {
            var sut = new EventProducerDelegate<State>(producer);

            IEnumerable<object> actual = sut.ProduceEvents(state, command);

            actual.Should().BeEquivalentTo(new[]
            {
                new ValueChanged(state.Value - 1),
                new ValueChanged(state.Value - 2),
            });
        }
    }
}
