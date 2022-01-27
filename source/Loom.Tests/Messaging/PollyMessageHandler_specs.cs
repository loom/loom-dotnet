using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Loom.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;

namespace Loom.Messaging
{
    [TestClass]
    public class PollyMessageHandler_specs
    {
        [TestMethod]
        [InlineAutoData(true)]
        [InlineAutoData(false)]
        public void sut_delegates_CanHandle(
            bool answer,
            IMessageHandler handler,
            Message message,
            IAsyncPolicy policy)
        {
            Mock.Get(handler).Setup(x => x.CanHandle(message)).Returns(answer);
            PollyMessageHandler sut = new(policy, handler);

            bool actual = sut.CanHandle(message);

            actual.Should().Be(answer);
        }

        [TestMethod, AutoData]
        public async Task sut_delegates_Handle(Message message)
        {
            MessageHandlerDouble spy = new();
            IAsyncPolicy policy = Policy.Handle<Exception>().RetryAsync();
            PollyMessageHandler sut = new(policy, handler: spy);

            await sut.Handle(message, cancellationToken: default);

            spy.Log.Should().ContainSingle().Which.Should().BeSameAs(message);
        }

        [TestMethod, AutoData]
        public async Task sut_retries_if_handler_fails(
            Exception[] exceptions,
            Message message)
        {
            MessageHandlerDouble spy = new(exceptions);
            IAsyncPolicy policy = Policy
                .Handle<Exception>()
                .RetryAsync(retryCount: exceptions.Length);
            PollyMessageHandler sut = new(policy, handler: spy);

            await sut.Handle(message, cancellationToken: default);

            spy.Log.Should().ContainSingle().Which.Should().BeSameAs(message);
        }

        public class MessageHandlerDouble : IMessageHandler
        {
            private readonly Queue<Exception> _exceptions;
            private readonly Queue<Message> _log = new();

            public MessageHandlerDouble(params Exception[] exceptions)
            {
                _exceptions = new(exceptions);
            }

            public bool CanHandle(Message message) => true;

            public IEnumerable<Message> Log => _log;

            public async Task Handle(Message message, CancellationToken cancellationToken)
            {
                await Task.Delay(1, cancellationToken);

                if (_exceptions.TryDequeue(out Exception? exception))
                {
                    throw exception;
                }

                _log.Enqueue(message);
            }
        }
    }
}
