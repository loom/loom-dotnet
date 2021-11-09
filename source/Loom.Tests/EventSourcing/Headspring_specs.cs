using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Loom.EventSourcing.InMemory;
using Loom.Messaging;
using Loom.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Loom.EventSourcing
{
    [TestClass]
    public class Headspring_specs
    {
        [TestMethod]
        public void sut_inherits_Rehydrator()
        {
            typeof(Headspring<State1>).Should().BeDerivedFrom<Rehydrator<State1>>();
        }

        [TestMethod]
        public void sut_implements_IMessageHandler()
        {
            typeof(Headspring<>).Should().Implement<IMessageHandler>();
        }

        [TestMethod]
        public void sut_is_abstract()
        {
            typeof(Headspring<>).Should().BeAbstract();
        }

        public class Sut :
            Headspring<State1>,
            IEventHandler<State1, Event1>,
            IEventHandler<State1, Event2>,
            IEventHandler<State2, Event4>,
            IEventProducer<State1, Command1>,
            IEventProducer<State1, Command2>,
            IEventProducer<State2, Command4>
        {
            private readonly Func<State1, Event1, State1> _handler1;
            private readonly Func<State1, Event2, State1> _handler2;
            private readonly Func<State1, Command1, IEnumerable<object>> _producer1;
            private readonly Func<State1, Command2, IEnumerable<object>> _producer2;

            public Sut(
                Func<string, State1> seedFactory,
                IMessageBus eventBus,
                Func<State1, Event1, State1> handler1 = null,
                Func<State1, Event2, State1> handler2 = null,
                Func<State1, Command1, IEnumerable<object>> producer1 = null,
                Func<State1, Command2, IEnumerable<object>> producer2 = null)
                : base(seedFactory, new InMemoryEventStore<State1>(eventBus))
            {
                _handler1 = handler1 ?? ((state, pastEvent) => state);
                _handler2 = handler2 ?? ((state, pastEvent) => state);
                _producer1 = producer1 ?? ((state, command) => Enumerable.Empty<object>());
                _producer2 = producer2 ?? ((state, command) => Enumerable.Empty<object>());
            }

            public State1 HandleEvent(State1 state, Event1 pastEvent)
                => _handler1.Invoke(state, pastEvent);

            public State1 HandleEvent(State1 state, Event2 pastEvent)
                => _handler2.Invoke(state, pastEvent);

            public State2 HandleEvent(State2 state, Event4 pastEvent) => state;

            public IEnumerable<object> ProduceEvents(State1 state, Command1 command)
                => _producer1.Invoke(state, command);

            public IEnumerable<object> ProduceEvents(State1 state, Command2 command)
                => _producer2.Invoke(state, command);

            public IEnumerable<object> ProduceEvents(State2 state, Command4 command)
                => Enumerable.Empty<object>();
        }

        [TestMethod, AutoData]
        public void sut_accepts_supported_command(
            Sut sut,
            Message<StreamCommand<Command1>> message)
        {
            bool actual = sut.CanHandle(message);
            actual.Should().BeTrue();
        }

        [TestMethod, AutoData]
        public void sut_does_not_accept_unsupported_command(
            Sut sut,
            Message<StreamCommand<Command3>> message)
        {
            bool actual = sut.CanHandle(message);
            actual.Should().BeFalse();
        }

        [TestMethod, AutoData]
        public void sut_does_not_accept_command_supported_by_interface_with_unknown_state_type(
            Sut sut,
            Message<StreamCommand<Command4>> message)
        {
            bool actual = sut.CanHandle(message);
            actual.Should().BeFalse();
        }

        [TestMethod, AutoData]
        public async Task sut_fails_to_handle_unknown_message(
            Sut sut,
            Message message)
        {
            Func<Task> action = () => sut.Handle(message);
            await action.Should().ThrowAsync<InvalidOperationException>();
        }

        [TestMethod, AutoData]
        public async Task sut_collects_events(
            MessageBusDouble spy,
            Message<StreamCommand<Command1>> message)
        {
            Sut sut = new(_ => new State1(Value: default), eventBus: spy);
            await sut.Handle(message);
            spy.Calls.Should().ContainSingle();
        }

        [TestMethod, AutoData]
        public async Task sut_collects_all_events_correctly(
            MessageBusDouble spy,
            Message<StreamCommand<Command1>> message)
        {
            Sut sut = new(seedFactory: _ => new State1(Value: default),
                          eventBus: spy,
                          producer1: (state, command) => new object[]
                          {
                              new Event1(command.Value),
                              new Event2(command.Value),
                          });

            await sut.Handle(message);

            ImmutableArray<Message> actual = spy.Calls.Select(x => x.Messages).Single();
            actual.Should().HaveCount(2);
            StreamCommand<Command1> data = message.data;
            actual[0].Data.Should().BeOfType<StreamEvent<Event1>>().And.BeEquivalentTo(data);
            actual[1].Data.Should().BeOfType<StreamEvent<Event2>>().And.BeEquivalentTo(data);
        }

        [TestMethod, AutoData]
        public async Task sut_sets_metadata_correctly(
            MessageBusDouble spy,
            Message<StreamCommand<Command1>> message)
        {
            Sut sut = new(
                seedFactory: _ => new State1(Value: default),
                eventBus: spy,
                producer1: (state, command) => new[] { new Event1(command.Value) });

            await sut.Handle(message);

            Message actual = spy.Calls.Select(x => x.Messages).Single()[0];
            actual.ProcessId.Should().Be(message.ProcessId);
            actual.Initiator.Should().Be(message.Initiator);
            actual.PredecessorId.Should().Be(message.Id);
        }

        [TestMethod, AutoData]
        public async Task sut_fails_to_raise_unsupported_event(
            IMessageBus eventBus,
            Message<StreamCommand<Command1>> message)
        {
            Sut sut = new(seedFactory: _ => new State1(Value: default),
                          eventBus,
                          producer1: (state, command) => new object[]
                          {
                              new Event1(command.Value),
                              new Event3(Value: "foo"),
                          });

            Func<Task> action = () => sut.Handle(message);

            await action.Should().ThrowAsync<InvalidOperationException>();
        }

        [TestMethod, AutoData]
        public async Task sut_fails_to_raise_event_supported_by_interface_with_unknown_state_type(
            IMessageBus eventBus,
            Message<StreamCommand<Command1>> message)
        {
            Sut sut = new(seedFactory: _ => new State1(Value: default),
                          eventBus,
                          producer1: (state, command) => new object[]
                          {
                              new Event1(command.Value),
                              new Event4(Value: default),
                          });

            Func<Task> action = () => sut.Handle(message);

            await action.Should().ThrowAsync<InvalidOperationException>();
        }

        [TestMethod, AutoData]
        public async Task sut_correctly_restores_state(
            MessageBusDouble spy,
            string messageId,
            string processId,
            string initiator,
            string predecessorId,
            string streamId,
            Command1 command1,
            Command2 command2)
        {
            // Arrange
            Sut sut = new(
                seedFactory: streamId => new(Hash(streamId)),
                eventBus: spy,
                handler1: (state, pastEvent) => new(pastEvent.Value),
                producer1: (state, command) => new[] { new Event1(state.Value + command.Value) },
                producer2: (state, command) => new[] { new Event1(state.Value + command.Value) });

            var data1 = StreamCommand.Create(streamId, command1);
            var data2 = StreamCommand.Create(streamId, command2);

            await sut.Handle(new(messageId, processId, initiator, predecessorId, data1));

            spy.Clear();

            // Act
            await sut.Handle(new(messageId, processId, initiator, predecessorId, data2));

            // Assert
            Message message = spy.Calls.Select(x => x.Messages).Single()[0];
            StreamEvent<Event1> actual = message.Data.As<StreamEvent<Event1>>();
            actual.Payload.Value.Should().Be(Hash(streamId) + command1.Value + command2.Value);
        }

        private static int Hash(object streamId) => streamId.GetHashCode();
    }
}
