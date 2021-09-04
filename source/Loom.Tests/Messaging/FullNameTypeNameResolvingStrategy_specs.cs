using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Loom.Messaging
{
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
        public void TryResolveTypeName_returns_full_name_of_type()
        {
            Type type = typeof(ReferencedType);
            var sut = new FullNameTypeNameResolvingStrategy();

            string actual = sut.TryResolveTypeName(type);

            actual.Should().Be(type.FullName);
        }
    }
}
