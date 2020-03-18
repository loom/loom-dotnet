namespace Loom.Messaging.Processes
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using FluentAssertions;
    using Loom.Testing;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ProcessOptions_specs
    {
        [TestMethod]
        [DataRow(301)]
        [AutoDataRepeat]
        public void constructor_has_guard_for_timeout_range(
            [Range(301, int.MaxValue)] int seconds)
        {
            var timeout = TimeSpan.FromSeconds(seconds);
            Action action = () => new ProcessOptions(_ => true, timeout);
            action.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
