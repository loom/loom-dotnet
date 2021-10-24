using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Loom.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Loom.Messaging
{
    [TestClass]
    public class CompositeMessageBus_specs
    {
        [TestMethod]
        public void sut_implements_IMessageBus()
        {
            typeof(CompositeMessageBus).Should().Implement<IMessageBus>();
        }

        [TestMethod, AutoData]
        public async Task Send_replays_to_all_buses(
            IMessageBus[] buses,
            Message[] messages,
            string partitionKey,
            CancellationToken cancellationToken)
        {
            // Arrange
            var sut = new CompositeMessageBus(buses);

            // Act
            await sut.Send(messages, partitionKey, cancellationToken);

            // Assert
            foreach (IMessageBus bus in buses)
            {
                Mock.Get(bus).Verify(
                    x => x.Send(messages, partitionKey, cancellationToken),
                    Times.Once());
            }
        }

        [TestMethod, AutoData]
        public async Task Send_replays_to_all_buses_even_if_some_fails(
            IMessageBus[] buses,
            Message[] messages,
            string partitionKey,
            CancellationToken cancellationToken)
        {
            // Arrange
            Mock.Get(buses.Sample())
                .Setup(x => x.Send(messages, partitionKey, cancellationToken))
                .ThrowsAsync(new InvalidOperationException());

            var sut = new CompositeMessageBus(buses);

            // Act
            await TryCatchIgnore(() => sut.Send(messages, partitionKey, cancellationToken));

            // Assert
            foreach (IMessageBus bus in buses)
            {
                Mock.Get(bus).Verify(
                    x => x.Send(messages, partitionKey, cancellationToken),
                    Times.Once());
            }
        }

        private async Task TryCatchIgnore(Func<Task> action)
        {
            try
            {
                await action.Invoke();
            }
            catch
            {
            }
        }
    }
}
