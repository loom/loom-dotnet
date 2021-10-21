using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FluentAssertions;
using Loom.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Loom.Messaging
{
    [TestClass]
    public class DebouncingMessageHandler_specs
    {
        [TestMethod]
        [InlineAutoData(true)]
        [InlineAutoData(false)]
        public void CanHandle_replays_correctly(
            bool canHandle,
            [Frozen] IMessageHandler handler,
            Message message,
            DebouncingMessageHandler sut)
        {
            Mock.Get(handler).Setup(x => x.CanHandle(message)).Returns(canHandle);
            bool actual = sut.CanHandle(message);
            actual.Should().Be(canHandle);
        }

        [TestMethod, AutoData]
        public async Task given_data_is_non_debouncable_then_Handle_relays_directly(
            Message message,
            [Frozen] IDebouncer debouncer,
            [Frozen] IMessageHandler handler,
            DebouncingMessageHandler sut)
        {
            // Act
            await sut.Handle(message);

            // Assert
            Mock.Get(handler).Verify(x => x.Handle(message), Times.Once());

            Expression<Func<IDebouncer, Task<bool>>> callTryConsume = x => x.TryConsume(
                It.IsAny<IDebouncable>(),
                It.IsAny<Func<IDebouncable, Task>>());
            Mock.Get(debouncer).Verify(callTryConsume, Times.Never());
        }

        [TestMethod, AutoData]
        public async Task given_data_is_debouncable_and_alive_then_Handle_relays(
            string id,
            IDebouncable debouncable,
            TracingProperties tracingProperties,
            Debouncer debouncer,
            IMessageHandler handler)
        {
            await debouncer.Register(debouncable);
            var sut = new DebouncingMessageHandler(debouncer, handler);
            var message = Message.Create(id, debouncable, tracingProperties);

            await sut.Handle(message);

            Mock.Get(handler).Verify(x => x.Handle(message), Times.Once());
        }

        [TestMethod, AutoData]
        public async Task given_data_is_debouncable_and_expired_then_Handle_relays(
            string id,
            IDebouncable debouncable,
            TracingProperties tracingProperties,
            Debouncer debouncer,
            IMessageHandler handler)
        {
            var sut = new DebouncingMessageHandler(debouncer, handler);
            var message = Message.Create(id, debouncable, tracingProperties);

            await sut.Handle(message);

            Mock.Get(handler).Verify(x => x.Handle(message), Times.Never());
        }

        public class Debouncer : IDebouncer
        {
            private IDebouncable _debouncable;

            public Task Register(IDebouncable debouncable)
            {
                _debouncable = debouncable;
                return Task.CompletedTask;
            }

            public async Task<bool> TryConsume<T>(T debouncable, Func<T, Task> consumer)
                where T : IDebouncable
            {
                if (ReferenceEquals(debouncable, _debouncable))
                {
                    await consumer.Invoke(debouncable);
                    return true;
                }

                return false;
            }
        }
    }
}
