namespace Loom.Messaging
{
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Loom.Testing;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class ImmediateMessageBus_specs
    {
        [TestMethod]
        public void sut_implements_IMessageBus()
        {
            typeof(ImmediateMessageBus).Should().Implement<IMessageBus>();
        }

        [TestMethod, AutoData]
        public async Task Send_handles_messages_immediately(
            IMessageHandler handler, Message[] messages, string partitionKey)
        {
            Message[] sample = messages.Sample(2).ToArray();
            var mock = Mock.Get(handler);
            sample.ForEach(m => mock.Setup(x => x.Accepts(m)).Returns(true));
            var sut = new ImmediateMessageBus(handler);

            await sut.Send(messages, partitionKey);

            mock.Verify(x => x.Handle(It.IsAny<Message>()), Times.Exactly(sample.Length));
            sample.ForEach(m => mock.Verify(x => x.Handle(m)));
        }
    }
}
