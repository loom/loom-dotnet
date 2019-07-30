namespace Loom.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Loom.Testing;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class FilteredMessageBus_specs
    {
        [TestMethod]
        public void sut_implements_IMessageBus()
        {
            typeof(FilteredMessageBus).Should().Implement<IMessageBus>();
        }

        [TestMethod, AutoData]
        public async Task sut_sends_only_filtered_messages(
            IMessageBus bus, Message[] messages, string partitionKey)
        {
            Message[] sample = messages.Where(x => x.GetHashCode() % 2 == 0).ToArray();
            Func<Message, bool> predicate = sample.Contains;
            var sut = new FilteredMessageBus(predicate, bus);

            await sut.Send(messages, partitionKey);

            Expression<Func<IMessageBus, Task>> call = x => x.Send(It.Is<IEnumerable<Message>>(p => p.SequenceEqual(sample)), partitionKey);
            Mock.Get(bus).Verify(expression: call, Times.Once());
        }
    }
}
