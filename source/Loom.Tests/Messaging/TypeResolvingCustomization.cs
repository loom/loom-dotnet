using AutoFixture;

namespace Loom.Messaging
{
    public class TypeResolvingCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Inject<ITypeNameResolvingStrategy>(new FullNameTypeNameResolvingStrategy());
            fixture.Inject<ITypeResolvingStrategy>(new CachingTypeResolvingStrategy(new TypeResolvingStrategy()));
        }
    }
}
