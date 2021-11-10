using System;
using FluentAssertions;
using Loom.Messaging;
using Loom.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Loom.EventSourcing
{
    [TestClass]
    public class InvariantViolated_specs
    {
        [TestMethod, AutoData]
        public void factory_method_creates_instance_with_error_correctly(
            StreamCommand<Command1> command, ActivityError error)
        {
            var actual = InvariantViolated.Create(command, error);

            actual.Should().NotBeNull();
            actual.Command.Should().BeEquivalentTo(command);
            actual.Error.Should().BeEquivalentTo(error);
        }

        [TestMethod, AutoData]
        public void factory_method_creates_instance_with_exception_correctly(
            StreamCommand<Command1> command, Exception exception)
        {
            var expectedError = new ActivityError(
                exception.GetType().FullName,
                exception.Message,
                exception.StackTrace);

            var actual = InvariantViolated.Create(command, exception);

            actual.Should().NotBeNull();
            actual.Command.Should().BeEquivalentTo(command);
            actual.Error.Should().BeEquivalentTo(expectedError);
        }
    }
}
