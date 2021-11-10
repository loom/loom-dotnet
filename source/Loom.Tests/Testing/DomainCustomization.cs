using AutoFixture;
using AutoFixture.AutoMoq;
using Loom.Json;
using Loom.Messaging;
using Loom.Messaging.Azure;

namespace Loom.Testing
{
    public class DomainCustomization : CompositeCustomization
    {
        public DomainCustomization()
            : base(new AutoMoqCustomization(),
                   new DateTimeCustomization(),
                   new JsonProcessorCustomization(),
                   new TypeResolvingCustomization(),
                   new EventConverterCustomization())
        {
        }
    }
}
