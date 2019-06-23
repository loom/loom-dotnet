namespace Loom.Messaging
{
    using System.Threading.Tasks;
    using FluentAssertions;
    using Loom.Testing;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class MessageHandler_specs
    {
        [TestMethod]
        public void sut_is_abstract()
        {
            typeof(MessageHandler<>).Should().BeAbstract();
        }

        [TestMethod]
        public void sut_implements_IMessageHandler()
        {
            typeof(MessageHandler<>).Should().Implement<IMessageHandler>();
        }

        [TestMethod, AutoData]
        public void CanHandle_returns_true_if_message_data_is_T(
            MessageHandler<MessageData1> sut, string id, MessageData1 data)
        {
            var message = new Message(id, data, tracingProperties: default);
            bool actual = sut.CanHandle(message);
            actual.Should().BeTrue();
        }

        [TestMethod, AutoData]
        public void CanHandle_returns_false_if_message_data_is_not_T(
            MessageHandler<MessageData1> sut, string id, MessageData2 data)
        {
            var message = new Message(id, data, tracingProperties: default);
            bool actual = sut.CanHandle(message);
            actual.Should().BeFalse();
        }

        [TestMethod, AutoData]
        public async Task Handle_relays_to_typed_method(
            MessageHandler<MessageData1> sut,
            string id,
            MessageData1 data,
            TracingProperties tracingProperties)
        {
            var message = new Message(id, data, tracingProperties);
            await sut.Handle(message);
            Mock.Get(sut).Verify(x => x.HandleMessage(id, data, tracingProperties), Times.Once());
        }
    }
}
