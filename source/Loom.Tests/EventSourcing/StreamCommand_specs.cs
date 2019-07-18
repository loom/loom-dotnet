namespace Loom.EventSourcing
{
    using System;
    using FluentAssertions;
    using Loom.Testing;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class StreamCommand_specs
    {
        [TestMethod]
        public void sut_is_sealed()
        {
            typeof(StreamCommand<>).Should().BeSealed();
        }

        [TestMethod, AutoData]
        public void factory_creates_instance_correctly(Guid streamId, Command1 payload)
        {
            StreamCommand<Command1> actual = StreamCommand.Create(streamId, payload);

            actual.StreamId.Should().Be(streamId);
            actual.Payload.Should().Be(payload);
        }
    }
}
