using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Loom.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Loom.Messaging
{
    [TestClass]
    public class MessageHandlerT_specs
    {
        [TestMethod]
        [InlineAutoData(true)]
        [InlineAutoData(false)]
        public void CanHandle_invokes_protected_method_when_T_is_assignable_from_data(
            bool result,
            MessageHandler<MessageData2> sut,
            string id,
            MessageData3 data,
            TracingProperties tracingProperties)
        {
            Expression arg = ItExpr.Is<Message<MessageData2>>(x => ReferenceEquals(x.Data, data));
            Mock.Get(sut).Protected().Setup<bool>("CanHandle", arg).Returns(result).Verifiable();

            bool actual = sut.CanHandle(Message.Create(id, data, tracingProperties));

            Mock.Get(sut).VerifyAll();
            actual.Should().Be(result);
        }

        [TestMethod, AutoData]
        public void protected_CanHandle_returns_true(
            MessageHandler<MessageData1> sut,
            string id,
            MessageData1 data,
            TracingProperties tracingProperties)
        {
            bool actual = sut.CanHandle(Message.Create(id, data, tracingProperties));
            actual.Should().BeTrue();
        }

        [TestMethod, AutoData]
        public void CanHandle_returns_false_when_T_is_not_assignable_from_data(
            MessageHandler<MessageData3> sut,
            string id,
            MessageData2 data,
            TracingProperties tracingProperties)
        {
            bool actual = sut.CanHandle(Message.Create(id, data, tracingProperties));
            actual.Should().BeFalse();
        }

        [TestMethod, AutoData]
        public void Handle_invokes_protected_method_when_T_is_assignable_from_data(
            MessageHandler<MessageData2> sut,
            string id,
            MessageData3 data,
            TracingProperties tracingProperties,
            Task result)
        {
            Expression arg = ItExpr.Is<Message<MessageData2>>(x => ReferenceEquals(x.Data, data));
            Mock.Get(sut).Protected().Setup<Task>("Handle", arg, CancellationToken.None).Returns(result).Verifiable();

            Task actual = sut.Handle(Message.Create(id, data, tracingProperties));

            Mock.Get(sut).VerifyAll();
            actual.Should().BeSameAs(result);
        }

        [TestMethod, AutoData]
        public async Task Handle_does_not_invoke_protected_method_when_T_is_not_assignable_from_data(
            MessageHandler<MessageData3> sut,
            string id,
            MessageData2 data,
            TracingProperties tracingProperties)
        {
            await sut.Handle(Message.Create(id, data, tracingProperties));

            object[] args = new object[]
            {
                ItExpr.IsAny<Message<MessageData3>>(),
                ItExpr.IsAny<CancellationToken>(),
            };
            Mock.Get(sut).Protected().Verify("Handle", Times.Never(), args);
        }
    }
}
