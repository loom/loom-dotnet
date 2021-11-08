using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Loom.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Loom.Messaging
{
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
            IMessageHandler handler,
            Message[] messages,
            string partitionKey,
            CancellationToken cancellationToken)
        {
            // Arrange
            Message[] sample = messages.Sample(2).ToArray();
            var mock = Mock.Get(handler);
            sample.ForEach(m => mock.Setup(x => x.CanHandle(m)).Returns(true));
            var sut = new ImmediateMessageBus(handler);

            // Act
            await sut.Send(messages, partitionKey, cancellationToken);

            // Assert
            mock.Verify(
                x => x.Handle(It.IsAny<Message>(), cancellationToken),
                Times.Exactly(sample.Length));

            foreach (Message message in sample)
            {
                mock.Verify(x => x.Handle(message, cancellationToken));
            }
        }
    }
}
