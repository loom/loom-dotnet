using System;
using System.Collections.Concurrent;
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
    public class SequentialCompositeMessageHandler_specs
    {
        [TestMethod]
        public void sut_implements_IMessageHandler()
        {
            typeof(SequentialCompositeMessageHandler).Should().Implement<IMessageHandler>();
        }

        [TestMethod, AutoData]
        public void sut_do_not_accept_message_if_no_handler_accepts_it(
            IMessageHandler[] handlers, Message message)
        {
            var sut = new SequentialCompositeMessageHandler(handlers);
            bool actual = sut.CanHandle(message);
            actual.Should().BeFalse();
        }

        [TestMethod, AutoData]
        public void sut_accepts_message_if_some_handler_accepts_it(
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
        public async Task Handle_does_not_relay_to_handlers_not_accepting_message(
            IMessageHandler[] handlers,
            Message message,
            CancellationToken cancellationToken)
        {
            var sut = new SequentialCompositeMessageHandler(handlers);
            var some = handlers.OrderBy(x => x.GetHashCode()).Skip(1).ToList();
            some.ForEach(handler => Mock.Get(handler).Setup(x => x.CanHandle(message)).Returns(false));

            await sut.Handle(message, cancellationToken);

            foreach (IMessageHandler handler in some)
            {
                Mock.Get(handler).Verify(x => x.Handle(message, cancellationToken), Times.Never());
            }
        }
    }
}
