using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Loom.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polly;

namespace Loom.Messaging
{
    [TestClass]
    public class PollyCompositeMessageHandler_specs
    {
        [TestMethod]
        public void sut_inherits_CompositeMessageHandler()
        {
            typeof(PollyCompositeMessageHandler)
                .Should()
                .BeDerivedFrom<CompositeMessageHandler>();
        }

        [TestMethod, AutoData]
        public async Task sut_applies_policy_to_handler(Exception[] exceptions, Message message)
        {
            MessageHandlerDouble spy = new(exceptions);
            int retryCount = exceptions.Length;
            IAsyncPolicy policy = Policy.Handle<Exception>().RetryAsync(retryCount);
            PollyCompositeMessageHandler sut = new(policy, handlers: spy);

            await sut.Handle(message);

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

                if (_exceptions.TryDequeue(out Exception exception))
                {
                    throw exception;
                }

                _log.Enqueue(message);
            }
        }
    }
}
