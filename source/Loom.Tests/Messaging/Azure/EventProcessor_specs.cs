namespace Loom.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Loom.Testing;
    using Microsoft.Azure.EventHubs;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class EventProcessor_specs
    {
        [TestMethod, AutoData]
        public async Task sut_processes_event_correctly(
            IEventConverter converter,
            LoggingMessageHandler spy,
            string id,
            MessageData1 data,
            TracingProperties tracingProperties)
        {
            EventProcessor sut = new EventProcessorBuilder(converter, spy).Build();
            var message = new Message(id, data, tracingProperties);
            EventData eventData = converter.ConvertToEvent(message);

            await sut.Process(new[] { eventData });

            spy.Log.Should().ContainSingle();
            spy.Log.Single().Should().BeEquivalentTo(message);
        }

        [TestMethod, AutoData]
        public async Task given_unknown_type_then_sut_ignores_message(
            IEventConverter converter,
            LoggingMessageHandler spy,
            Message message,
            string unknownType)
        {
            EventProcessor sut = new EventProcessorBuilder(converter, spy).Build();
            EventData eventData = converter.ConvertToEvent(message);
            eventData.Properties["Type"] = unknownType;

            await sut.Process(new[] { eventData });

            spy.Log.Should().BeEmpty();
        }

        [TestMethod, AutoData]
        public async Task given_unhandlable_message_then_sut_ignores_it(
            IEventConverter converter,
            IMessageHandler handler,
            Message message)
        {
            Mock.Get(handler).Setup(x => x.CanHandle(It.IsAny<Message>())).Returns(false);
            EventProcessor sut = new EventProcessorBuilder(converter, handler).Build();
            EventData eventData = converter.ConvertToEvent(message);

            await sut.Process(new[] { eventData });

            Mock.Get(handler).Verify(x => x.Handle(It.IsAny<Message>()), Times.Never());
        }

        [TestMethod, AutoData]
        public async Task sut_does_not_throw_aggregate_exception_for_bad_messages(
            IEventConverter converter,
            IMessageHandler handler,
            (Message message, Exception exception)[] tuples)
        {
            // Arrange
            EventProcessor sut = new EventProcessorBuilder(converter, handler).Build();

            var mock = Mock.Get(handler);
            foreach ((Message message, Exception exception) in tuples)
            {
                mock.Setup(x => x.CanHandle(It.Is<Message>(m => m.Id == message.Id))).Returns(true);
                mock.Setup(x => x.Handle(It.Is<Message>(m => m.Id == message.Id))).ThrowsAsync(exception);
            }

            IEnumerable<EventData> events = tuples
                .Select(t => t.message)
                .Select(converter.ConvertToEvent);

            // Act
            Func<Task> action = () => sut.Process(events);

            // Assert
            await action.Should().NotThrowAsync();
        }
    }
}
