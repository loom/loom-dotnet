namespace Loom.EventSourcing
{
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class StreamEvent_specs
    {
        [TestMethod]
        public void sut_is_sealed()
        {
            typeof(StreamEvent<>).Should().BeSealed();
        }
    }
}
