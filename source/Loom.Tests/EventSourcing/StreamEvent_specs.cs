namespace Loom.EventSourcing
{
    using FluentAssertions;
    using Loom.Messaging;
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

        [TestMethod]
        public void sut_implements_IPartitioned()
        {
            typeof(StreamEvent<>).Should().Implement<IPartitioned>();
        }

        [TestMethod, AutoData]
        public void PartitionKey_returns_StreamId_as_string(StreamEvent<Event1> sut)
        {
            string actual = sut.PartitionKey;
            actual.Should().Be($"{sut.StreamId}");
        }
    }
}
