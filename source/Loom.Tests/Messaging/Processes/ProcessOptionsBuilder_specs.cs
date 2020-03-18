namespace Loom.Messaging.Processes
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using FluentAssertions;
    using Loom.Testing;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ProcessOptionsBuilder_specs
    {
        [TestMethod]
        public void Build_creates_new_ProcessOptions_instance()
        {
            var sut = new ProcessOptionsBuilder();

            ProcessOptions[] actual = new[]
            {
                sut.Build(),
                sut.Build(),
                sut.Build(),
            };

            actual.Should().OnlyHaveUniqueItems();
        }

        [TestMethod, AutoData]
        public void WithTimeout_returns_self(TimeSpan timeout)
        {
            var sut = new ProcessOptionsBuilder();
            ProcessOptionsBuilder actual = sut.WithTimeout(timeout);
            actual.Should().BeSameAs(sut);
        }

        [TestMethod, AutoData]
        public void WithTimeout_correctly_sets_Timeout_property(TimeSpan timeout)
        {
            var sut = new ProcessOptionsBuilder();
            ProcessOptions actual = sut.WithTimeout(timeout).Build();
            actual.Timeout.Should().Be(timeout);
        }

        [TestMethod]
        [DataRow(301)]
        [AutoDataRepeat]
        public void WithTimeout_has_guard_for_timeout_range(
            [Range(301, int.MaxValue)] int seconds)
        {
            var timeout = TimeSpan.FromSeconds(seconds);
            var sut = new ProcessOptionsBuilder();

            Action action = () => sut.WithTimeout(timeout);

            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestMethod]
        public void default_timeout_is_one_minute()
        {
            var sut = new ProcessOptionsBuilder();
            ProcessOptions actual = sut.Build();
            actual.Timeout.Should().Be(TimeSpan.FromMinutes(1));
        }

        [TestMethod]
        public void WaitFor_returns_self()
        {
            var sut = new ProcessOptionsBuilder();
            ProcessOptionsBuilder actual = sut.WaitFor<CompletionEvent1>();
            actual.Should().BeSameAs(sut);
        }

        [TestMethod]
        public void WaitFor_correctly_applies_type_filter_to_CompletionDeterminer()
        {
            var sut = new ProcessOptionsBuilder();

            ProcessOptions options = sut.WaitFor<CompletionEvent1>().Build();

            options.CompletionDeterminer.Invoke(new CompletionEvent1()).Should().BeTrue();
            options.CompletionDeterminer.Invoke(new object()).Should().BeFalse();
        }

        [TestMethod]
        public void WaitFor_accumulates_type_filter()
        {
            var sut = new ProcessOptionsBuilder();

            ProcessOptions options = sut
                .WaitFor<CompletionEvent1>()
                .WaitFor<CompletionEvent2>()
                .Build();

            options.CompletionDeterminer.Invoke(new CompletionEvent1()).Should().BeTrue();
            options.CompletionDeterminer.Invoke(new CompletionEvent2()).Should().BeTrue();
            options.CompletionDeterminer.Invoke(new object()).Should().BeFalse();
        }

        public class CompletionEvent1
        {
        }

        public class CompletionEvent2
        {
        }
    }
}
