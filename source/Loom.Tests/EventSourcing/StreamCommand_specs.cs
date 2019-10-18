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
        public void factory_creates_instance_correctly(
            Guid streamId, Command1 payload)
        {
            var actual = StreamCommand.Create(streamId, payload);

            actual.StreamId.Should().Be(streamId);
            actual.Payload.Should().Be(payload);
        }

        [TestMethod, AutoData]
        public void TryDecompose_returns_true_if_value_is_stream_command(
            StreamCommand<Command1> value)
        {
            bool actual = StreamCommand.TryDecompose(value, out _, out _);
            actual.Should().BeTrue();
        }

        [TestMethod, AutoData]
        public void TryDecompose_returns_false_if_value_is_non_stream_command(
            StreamEvent<Event1> value)
        {
            bool actual = StreamCommand.TryDecompose(value, out _, out _);
            actual.Should().BeFalse();
        }

        [TestMethod]
        public void TryDecompose_returns_false_if_value_is_null()
        {
            bool actual = StreamCommand.TryDecompose(value: null, out _, out _);
            actual.Should().BeFalse();
        }

        [TestMethod, AutoData]
        public void TryDecompose_returns_stream_id_correctly(
            StreamCommand<Command1> value)
        {
            _ = StreamCommand.TryDecompose(value, out Guid streamId, out _);
            streamId.Should().Be(value.StreamId);
        }

        [TestMethod, AutoData]
        public void TryDecompose_returns_payload_correctly(
            StreamCommand<Command1> value)
        {
            _ = StreamCommand.TryDecompose(value, out _, out object payload);
            payload.Should().BeSameAs(value.Payload);
        }
    }
}
