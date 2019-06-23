namespace Loom.Messaging
{
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Loom.Testing;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class CompositeMessageHandler_specs
    {
        [TestMethod]
        public void sut_implements_IMessageHandler()
        {
            typeof(CompositeMessageHandler).Should().Implement<IMessageHandler>();
        }

        [TestMethod, AutoData]
        public void if_all_handlers_cannot_handle_message_CanHandle_returns_false(
            IMessageHandler[] handlers, Message message)
        {
            var sut = new CompositeMessageHandler(handlers);
            bool actual = sut.CanHandle(message);
            actual.Should().BeFalse();
        }

        [TestMethod, AutoData]
        public void if_some_handler_can_handle_message_CanHandle_returns_true(
            IMessageHandler[] handlers, Message message)
        {
            var sut = new CompositeMessageHandler(handlers);
            IMessageHandler some = handlers.OrderBy(x => x.GetHashCode()).First();
            Mock.Get(some).Setup(x => x.CanHandle(message)).Returns(true);

            bool actual = sut.CanHandle(message);

            actual.Should().BeTrue();
        }

        [TestMethod, AutoData]
        public async Task Handle_relays_to_handlers_able_to_handle_message(
            IMessageHandler[] handlers, Message message)
        {
            var sut = new CompositeMessageHandler(handlers);
            var some = handlers.OrderBy(x => x.GetHashCode()).Skip(1).ToList();
            some.ForEach(handler => Mock.Get(handler).Setup(x => x.CanHandle(message)).Returns(true));

            await sut.Handle(message);

            foreach (IMessageHandler handler in some)
            {
                Mock.Get(handler).Verify(x => x.Handle(message), Times.Once());
            }
        }

        [TestMethod, AutoData]
        public async Task Handle_does_not_relay_to_handlers_not_able_to_handle_message(
            IMessageHandler[] handlers, Message message)
        {
            var sut = new CompositeMessageHandler(handlers);
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
