using System;
using FluentAssertions;
using Loom.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Loom.EventSourcing
{
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

        [TestMethod, AutoData]
        public void TryDecompose_returns_true_if_value_is_stream_event(
            StreamEvent<Event1> value)
        {
            bool actual = StreamEvent.TryDecompose(value, out _, out _, out _, out _);
            actual.Should().BeTrue();
        }

        [TestMethod, AutoData]
        public void TryDecompose_returns_false_if_value_is_non_stream_event(
            StreamCommand<Command1> value)
        {
            bool actual = StreamEvent.TryDecompose(value, out _, out _, out _, out _);
            actual.Should().BeFalse();
        }

        [TestMethod]
        public void TryDecompose_returns_false_if_value_is_null()
        {
            bool actual = StreamEvent.TryDecompose(value: null, out _, out _, out _, out _);
            actual.Should().BeFalse();
        }

        [TestMethod, AutoData]
        public void TryDecompose_returns_stream_id_correctly(
            StreamEvent<Event1> value)
        {
            StreamEvent.TryDecompose(value, out Guid streamId, out _, out _, out _);
            streamId.Should().Be(value.StreamId);
        }

        [TestMethod, AutoData]
        public void TryDecompose_returns_version_correctly(
            StreamEvent<Event1> value)
        {
            StreamEvent.TryDecompose(value, out _, out long version, out _, out _);
            version.Should().Be(value.Version);
        }

        [TestMethod, AutoData]
        public void TryDecompose_returns_raised_time_correctly(
            StreamEvent<Event1> value)
        {
            StreamEvent.TryDecompose(value, out _, out _, out DateTime raisedTimeUtc, out _);
            raisedTimeUtc.Should().Be(value.RaisedTimeUtc);
        }

        [TestMethod, AutoData]
        public void TryDecompose_returns_payload_correctly(
            StreamEvent<Event1> value)
        {
            StreamEvent.TryDecompose(value, out _, out _, out _, out object payload);
            payload.Should().BeSameAs(value.Payload);
        }
    }
}
