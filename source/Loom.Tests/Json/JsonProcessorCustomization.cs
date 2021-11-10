using AutoFixture;
using Newtonsoft.Json;

namespace Loom.Json
{
    public class JsonProcessorCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            IJsonProcessor processor = new JsonProcessor(new JsonSerializer());
            fixture.Inject(processor);
        }
    }
}
