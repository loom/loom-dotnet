using AutoFixture;
using Loom.Json;
using Newtonsoft.Json;

namespace Loom.Messaging.Azure
{
    public class EventConverterCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Inject<IEventConverter>(
                new EventConverter(
                    new JsonProcessor(
                        new JsonSerializer()),
                    new TypeResolver(
                        new FullNameTypeNameResolvingStrategy(),
                        new CachingTypeResolvingStrategy(
                            new TypeResolvingStrategy()))));
        }
    }
}
