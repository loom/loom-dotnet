namespace Loom.Messaging
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Loom.Testing;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

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
            IMessageBus[] buses, Message[] messages, string partitionKey)
        {
            var sut = new CompositeMessageBus(buses);
            await sut.Send(messages, partitionKey);
            buses.Select(Mock.Get).ForEach(bus => bus.Verify(x => x.Send(messages, partitionKey), Times.Once()));
        }

        [TestMethod, AutoData]
        public async Task Send_replays_to_all_buses_even_if_some_fails(
            IMessageBus[] buses, Message[] messages, string partitionKey)
        {
            // Arrange
            Mock.Get(buses.Sample())
                .Setup(x => x.Send(messages, partitionKey))
                .ThrowsAsync(new InvalidOperationException());

            var sut = new CompositeMessageBus(buses);

            // Act
            await TryCatchIgnore(() => sut.Send(messages, partitionKey));

            // Assert
            buses.Select(Mock.Get).ForEach(bus => bus.Verify(x => x.Send(messages, partitionKey), Times.Once()));
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
