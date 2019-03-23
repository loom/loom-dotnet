namespace Loom.Messaging
{
    using System;
    using AutoFixture;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class FullNameTypeResolvingStrategy_specs
    {
        [TestMethod]
        public void sut_implements_ITypeResolvingStrategy()
        {
            Type sut = typeof(FullNameTypeResolvingStrategy);
            sut.Should().Implement<ITypeResolvingStrategy>();
        }

        [TestMethod]
        public void TryResolveType_returns_full_name_matching_type()
        {
            Type type = typeof(ReferencedType);
            string fullName = type.FullName;
            var sut = new FullNameTypeResolvingStrategy();
            var resolver = new TypeResolver(
                Mock.Of<ITypeNameResolvingStrategy>(), sut);

            Type actual = resolver.TryResolveType(fullName);

            actual.Should().Be(type);
        }

        [TestMethod]
        public void given_unknown_type_name_then_TryResolveType_returns_null()
        {
            string fullName = new Fixture().Create<string>();
            var sut = new FullNameTypeResolvingStrategy();
            var resolver = new TypeResolver(
                Mock.Of<ITypeNameResolvingStrategy>(), sut);

            Type actual = resolver.TryResolveType(fullName);

            actual.Should().BeNull();
        }
    }
}
