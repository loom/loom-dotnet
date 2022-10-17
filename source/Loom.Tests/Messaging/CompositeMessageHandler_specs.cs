using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Loom.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Loom.Messaging;

[TestClass]
public class CompositeMessageHandler_specs
{
    [TestMethod]
    public void sut_implements_IMessageHandler()
    {
        typeof(CompositeMessageHandler).Should().Implement<IMessageHandler>();
    }

    [TestMethod, AutoData]
    public void sut_does_not_accept_message_if_no_handlers_accepts_it(
        IMessageHandler[] handlers, Message message)
    {
        var sut = new CompositeMessageHandler(handlers);
        bool actual = sut.CanHandle(message);
        actual.Should().BeFalse();
    }

    [TestMethod, AutoData]
    public void sut_accepts_message_if_some_handler_accepts_it(
        IMessageHandler[] handlers, Message message)
    {
        var sut = new CompositeMessageHandler(handlers);
        IMessageHandler some = handlers.OrderBy(x => x.GetHashCode()).First();
        Mock.Get(some).Setup(x => x.CanHandle(message)).Returns(true);

        bool actual = sut.CanHandle(message);

        actual.Should().BeTrue();
    }

    [TestMethod, AutoData]
    public async Task Handle_relays_to_handlers_accepting_message(
        IMessageHandler[] handlers,
        Message message,
        CancellationToken cancellationToken)
    {
        var sut = new CompositeMessageHandler(handlers);
        var some = handlers.OrderBy(x => x.GetHashCode()).Skip(1).ToList();
        some.ForEach(handler => Mock.Get(handler).Setup(x => x.CanHandle(message)).Returns(true));

        await sut.Handle(message, cancellationToken);

        foreach (IMessageHandler handler in some)
        {
            Mock.Get(handler).Verify(x => x.Handle(message, cancellationToken), Times.Once());
        }
    }

    [TestMethod, AutoData]
    public async Task Handle_does_not_relay_to_handlers_not_accepting_message(
        IMessageHandler[] handlers,
        Message message,
        CancellationToken cancellationToken)
    {
        var sut = new CompositeMessageHandler(handlers);
        var some = handlers.OrderBy(x => x.GetHashCode()).Skip(1).ToList();
        foreach (IMessageHandler handler in some)
        {
            Mock.Get(handler).Setup(x => x.CanHandle(message)).Returns(false);
        }

        await sut.Handle(message, cancellationToken);

        foreach (IMessageHandler handler in some)
        {
            Mock.Get(handler).Verify(x => x.Handle(message, cancellationToken), Times.Never());
        }
    }

    [TestMethod, AutoData]
    public async Task sut_does_not_propagate_fault_of_CanHandle(
        Generator<IMessageHandler> generator,
        InvalidOperationException exception,
        Message message,
        CancellationToken cancellationToken)
    {
        // Arrange
        IMessageHandler[] fineHandlers = generator.Take(100).ToArray();
        foreach (IMessageHandler handler in fineHandlers)
        {
            Mock.Get(handler).Setup(x => x.CanHandle(message)).Returns(true);
        }

        IMessageHandler brokenHandler = generator.First();
        Mock.Get(brokenHandler).Setup(x => x.CanHandle(message)).Throws(exception);

        IMessageHandler[] handlers = fineHandlers.Append(brokenHandler).Shuffle().ToArray();
        CompositeMessageHandler sut = new(handlers);

        // Act
        Func<Task> action = () => sut.Handle(message, cancellationToken);

        // Assert
        await action.Should().ThrowAsync<Exception>();
        foreach (IMessageHandler handler in fineHandlers)
        {
            Mock.Get(handler).Verify(x => x.Handle(message, cancellationToken), Times.Once());
        }
    }

    [TestMethod, AutoData]
    public async Task sut_does_not_propagate_fault_of_Handle(
        Generator<IMessageHandler> generator,
        InvalidOperationException exception,
        Message message,
        CancellationToken cancellationToken)
    {
        // Arrange
        IMessageHandler[] fineHandlers = generator.Take(100).ToArray();

        IMessageHandler brokenHandler = generator.First();
        Mock.Get(brokenHandler).Setup(x => x.Handle(message, cancellationToken)).Throws(exception);

        IMessageHandler[] handlers = fineHandlers.Append(brokenHandler).Shuffle().ToArray();
        foreach (IMessageHandler handler in handlers)
        {
            Mock.Get(handler).Setup(x => x.CanHandle(message)).Returns(true);
        }

        CompositeMessageHandler sut = new(handlers);

        // Act
        Func<Task> action = () => sut.Handle(message, cancellationToken);

        // Assert
        await action.Should().ThrowAsync<Exception>();
        foreach (IMessageHandler handler in fineHandlers)
        {
            Mock.Get(handler).Verify(x => x.Handle(message, cancellationToken), Times.Once());
        }
    }
}
