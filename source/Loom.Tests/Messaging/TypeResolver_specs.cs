namespace Loom.Messaging
{
    using System;
    using System.Linq;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class TypeResolver_specs
    {
        [TestMethod]
        public void sut_is_sealed()
        {
            typeof(TypeResolver).Should().BeSealed();
        }

        private Type GetTypeFromReferencedAssembly() => typeof(ReferencedType);

        [TestMethod]
        public void ResolveTypeName_delegates_correctly()
        {
            // Arrange
            Type type = GetTypeFromReferencedAssembly();

            var sut = new TypeResolver(
                new DelegatingTypeNameResolvingStrategy(t => t.FullName),
                Mock.Of<ITypeResolvingStrategy>());

            // Act
            string actual = sut.ResolveTypeName(type);

            // Assert
            actual.Should().Be(type.FullName);
        }

        [TestMethod]
        public void TryResolveType_delegates_correctly()
        {
            // Arrange
            Type type = GetTypeFromReferencedAssembly();

            var typeResolvingStrategy = new DelegatingTypeResolvingStrategy(
                (types, typeName) => types.Single(t => t.FullName == typeName));

            var sut = new TypeResolver(
                Mock.Of<ITypeNameResolvingStrategy>(), typeResolvingStrategy);

            // Act
            Type actual = sut.TryResolveType(type.FullName);

            // Assert
            actual.Should().Be(type);
        }
    }
}
