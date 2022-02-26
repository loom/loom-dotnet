using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Loom.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Loom.Messaging;

[TestClass]
public class MessageBusExtensions_specs
{
    [TestMethod, AutoData]
    public async Task Send_calls_instance_method(
        MessageBusDouble spy,
        Message message,
        string partitionKey)
    {
        await spy.Send(message, partitionKey);
        spy.Calls.Should().ContainSingle();
    }

    [TestMethod, AutoData]
    public async Task Send_correctly_sends_single_message(
        MessageBusDouble spy,
        Message message,
        string partitionKey)
    {
        await spy.Send(message, partitionKey);

        ImmutableArray<Message> messages = spy.Calls.Single().Messages;
        messages.Should().ContainSingle().Which.Should().Be(message);
    }

    [TestMethod, AutoData]
    public async Task Send_sends_single_message_with_correct_partition_key(
        MessageBusDouble spy,
        Message message,
        string partitionKey)
    {
        await spy.Send(message, partitionKey);
        spy.Calls.Single().PartitionKey.Should().Be(partitionKey);
    }
}
