namespace Loom.EventSourcing
{
    using System;
    using FluentAssertions;
    using Loom.Messaging;
    using Loom.Testing;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class InvariantViolated_specs
    {
        [TestMethod, AutoData]
        public void factory_method_creates_instance_correctly(
            Guid streamId, Command1 command, ActivityError error)
        {
            var actual = InvariantViolated.Create(streamId, command, error);

            actual.Should().NotBeNull();
            actual.StreamId.Should().Be(streamId);
            actual.Command.Should().BeEquivalentTo(command);
            actual.Error.Should().BeEquivalentTo(error);
        }
    }
}
