using System;
using FluentAssertions;
using Loom.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Loom.EventSourcing
{
    [TestClass]
    public class StreamCommand_specs
    {
        [TestMethod]
        public void sut_is_sealed()
        {
            typeof(StreamCommand<>).Should().BeSealed();
        }

        [TestMethod, AutoData]
        public void factory_creates_instance_correctly(
            Guid streamId, Command1 payload)
        {
            var actual = StreamCommand.Create(streamId, payload);

            actual.StreamId.Should().Be(streamId);
            actual.Payload.Should().Be(payload);
        }
    }
}
