namespace Loom.Messaging
{
    using System;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class FullNameTypeNameResolvingStrategy_specs
    {
        [TestMethod]
        public void sut_implements_ITypeNameResolvingStrategy()
        {
            Type sut = typeof(FullNameTypeNameResolvingStrategy);
            sut.Should().Implement<ITypeNameResolvingStrategy>();
        }

        [TestMethod]
        public void ResolveTypeName_returns_full_name_of_type()
        {
            Type type = typeof(ReferencedType);
            var sut = new FullNameTypeNameResolvingStrategy();

            string actual = sut.ResolveTypeName(type);

            actual.Should().Be(type.FullName);
        }
    }
}
