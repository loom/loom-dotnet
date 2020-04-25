namespace Loom.Messaging.Processes
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Loom.Messaging;
    using Loom.Testing;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ProcessMessageWatcher_specs
    {
        [TestMethod]
        public void sut_implements_IMessageHandler()
        {
            typeof(ProcessMessageWatcher).Should().Implement<IMessageHandler>();
        }

        [TestMethod, AutoData]
        public void CanHandle_true_for_Message(
            IProcessMessageCollector dummy,
            Message message)
        {
            // Arrange
            var sut = new ProcessMessageWatcher(dummy);

            // Act
            bool actual = sut.CanHandle(message);

            // Assert
            actual.Should().BeTrue();
        }

        [TestMethod, AutoData]
        public async Task Handle_collects_message_correctly(
            Spy collector,
            Message message)
        {
            // Arrange
            var sut = new ProcessMessageWatcher(collector);

            // Act
            await sut.Handle(message);

            // Assert
            List<Message> actual = collector.Messages;
            actual.Should().HaveCount(1);
            actual[0].Should().BeEquivalentTo(message);
        }

        public class Spy : IProcessMessageCollector
        {
            public List<Message> Messages { get; } = new List<Message>();

            public Task Collect(
                Message message,
                CancellationToken cancellationToken)
            {
                Messages.Add(message);
                return Task.CompletedTask;
            }
        }
    }
}
