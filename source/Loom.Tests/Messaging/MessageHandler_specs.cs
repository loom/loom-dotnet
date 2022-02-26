using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Loom.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Loom.Messaging;

[TestClass]
public class MessageHandler_specs
{
    [TestMethod]
    public void sut_implments_IMessageHandler()
    {
        typeof(MessageHandler<>).Should().Implement<IMessageHandler>();
    }

    [TestMethod, AutoData]
    public void CanHandle_returns_true_for_data_of_matching_type(
        MessageHandler<Payload> sut,
        Message<Payload> source)
    {
        Message message = source;
        bool actual = sut.CanHandle(message);
        actual.Should().BeTrue();
    }

    [TestMethod, AutoData]
    public void CanHandle_returns_false_for_data_of_non_matching_type(
        MessageHandler<Payload> sut,
        Message message)
    {
        bool actual = sut.CanHandle(message);
        actual.Should().BeFalse();
    }

    [TestMethod, AutoData]
    public async Task Handle_fails_for_data_of_non_matching_type(
        MessageHandler<Payload> sut,
        Message message)
    {
        Func<Task> action = () => sut.Handle(message);
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [TestMethod, AutoData]
    public async Task Handle_correctly_invokes_internal_method(
        MessageLogger<Payload> sut,
        Message<Payload> source)
    {
        Message message = source;
        await sut.Handle(message);
        sut.Log.Should().ContainSingle().Which.Should().BeEquivalentTo(source);
    }

    public record Payload(string Name, int Value);

    public class MessageLogger<T> : MessageHandler<T>
        where T : notnull
    {
        private readonly ConcurrentQueue<Message<T>> _log = new();

        public IEnumerable<Message<T>> Log => _log;

        protected override Task Handle(
            Message<T> message,
            CancellationToken cancellationToken)
        {
            _log.Enqueue(message);
            return Task.CompletedTask;
        }
    }
}
