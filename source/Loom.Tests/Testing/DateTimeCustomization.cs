using System;
using System.Linq;
using System.Reflection;
using AutoFixture;
using AutoFixture.Kernel;

namespace Loom.Testing
{
    public class DateTimeCustomization : ICustomization
    {
        private static readonly Type[] DateTimeTypes = new[]
        {
            typeof(DateTime),
            typeof(DateTime?),
        };

        public void Customize(IFixture fixture)
        {
            fixture.Customizations.Add(new Builder(fixture));
        }

        private class Builder : ISpecimenBuilder
        {
            private readonly IFixture _fixture;

            public Builder(IFixture fixture) => _fixture = fixture;

            public object Create(object request, ISpecimenContext context)
            {
                if (request is ParameterInfo p &&
                    DateTimeTypes.Contains(p.ParameterType) &&
                    p.Name.EndsWith("UTC", StringComparison.OrdinalIgnoreCase))
                {
                    DateTime source = _fixture.Create<DateTime>();
                    return new DateTime(source.Ticks, DateTimeKind.Utc);
                }

                return new NoSpecimen();
            }
        }
    }
}
