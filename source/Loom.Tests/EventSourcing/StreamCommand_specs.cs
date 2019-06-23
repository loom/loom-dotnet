namespace Loom.EventSourcing
{
    using FluentAssertions;
    using Loom.Messaging;
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

        [TestMethod]
        public void sut_implements_IPartitioned()
        {
            typeof(StreamCommand<>).Should().Implement<IPartitioned>();
        }

        [TestMethod, AutoData]
        public void PartitionKey_returns_StreamId_as_string(StreamCommand<Command1> sut)
        {
            string actual = sut.PartitionKey;
            actual.Should().Be($"{sut.StreamId}");
        }
    }
}
