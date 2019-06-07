namespace Loom.Messaging
{
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class Message_specs
    {
        [TestMethod]
        public void sut_is_sealed()
        {
            typeof(Message).Should().BeSealed();
        }
    }
}
