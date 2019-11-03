namespace Loom.Messaging
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Loom.Testing;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class SequentialCompositeMessageHandler_specs
    {
        [TestMethod]
        public void sut_implements_IMessageHandler()
        {
            typeof(SequentialCompositeMessageHandler).Should().Implement<IMessageHandler>();
        }

        [TestMethod, AutoData]
        public void if_all_handlers_cannot_handle_message_CanHandle_returns_false(
            IMessageHandler[] handlers, Message message)
        {
            var sut = new SequentialCompositeMessageHandler(handlers);
            bool actual = sut.CanHandle(message);
            actual.Should().BeFalse();
        }

        [TestMethod, AutoData]
        public void if_some_handler_can_handle_message_CanHandle_returns_true(
            IMessageHandler[] handlers, Message message)
        {
            var sut = new SequentialCompositeMessageHandler(handlers);
            IMessageHandler some = handlers.OrderBy(x => x.GetHashCode()).First();
            Mock.Get(some).Setup(x => x.CanHandle(message)).Returns(true);

            bool actual = sut.CanHandle(message);

            actual.Should().BeTrue();
        }

        [TestMethod, AutoData]
        public async Task Handle_relays_sequentially(
            Random random,
            ConcurrentQueue<int> log,
            Message message)
        {
            // Arrange
            var handlers = new IMessageHandler[10];
            for (int i = 0; i < handlers.Length; i++)
            {
                int order = i;
                handlers[i] = new DelegatingMessageHandler(async _ =>
                {
                    await Task.Delay(millisecondsDelay: random.Next(100, 1000));
                    log.Enqueue(order);
                });
            }

            var sut = new SequentialCompositeMessageHandler(handlers);

            // Act
            await sut.Handle(message);

            // Assert
            log.Should().BeInAscendingOrder();
        }

        [TestMethod, AutoData]
        public async Task Handle_does_not_relay_to_handlers_not_able_to_handle_message(
            IMessageHandler[] handlers, Message message)
        {
            var sut = new SequentialCompositeMessageHandler(handlers);
            var some = handlers.OrderBy(x => x.GetHashCode()).Skip(1).ToList();
            some.ForEach(handler => Mock.Get(handler).Setup(x => x.CanHandle(message)).Returns(false));

            await sut.Handle(message);

            foreach (IMessageHandler handler in some)
            {
                Mock.Get(handler).Verify(x => x.Handle(message), Times.Never());
            }
        }
    }
}
