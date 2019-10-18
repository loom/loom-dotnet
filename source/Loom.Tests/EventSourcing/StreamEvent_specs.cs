namespace Loom.EventSourcing
{
    using System;
    using FluentAssertions;
    using Loom.Testing;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class StreamEvent_specs
    {
        [TestMethod]
        public void sut_is_sealed()
        {
            typeof(StreamEvent<>).Should().BeSealed();
        }

        [TestMethod, AutoData]
        public void factory_creates_instance_correctly(
            Guid streamId,
            long version,
            DateTime raisedTimeUtc,
            Event1 payload)
        {
            var actual = StreamEvent.Create(streamId, version, raisedTimeUtc, payload);

            actual.StreamId.Should().Be(streamId);
            actual.Version.Should().Be(version);
            actual.RaisedTimeUtc.Should().Be(raisedTimeUtc);
            actual.Payload.Should().Be(payload);
        }
    }
}
