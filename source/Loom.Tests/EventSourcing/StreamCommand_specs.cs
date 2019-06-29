namespace Loom.EventSourcing
{
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class StreamCommand_specs
    {
        [TestMethod]
        public void sut_is_sealed()
        {
            typeof(StreamCommand<>).Should().BeSealed();
        }
    }
}
