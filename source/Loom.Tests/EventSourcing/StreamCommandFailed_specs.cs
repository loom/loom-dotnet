namespace Loom.EventSourcing
{
    using FluentAssertions;
    using Loom.Testing;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;

    [TestClass]
    public class StreamCommandFailed_specs
    {
        [TestMethod, AutoData]
        public void sut_is_json_serializable(StreamCommandFailed<Command1> sut)
        {
            string json = JsonConvert.SerializeObject(sut);
            StreamCommandFailed<Command1> actual = JsonConvert.DeserializeObject<StreamCommandFailed<Command1>>(json);
            actual.Should().BeEquivalentTo(sut);
        }
    }
}
