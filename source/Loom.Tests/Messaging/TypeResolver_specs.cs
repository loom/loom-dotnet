using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Loom.Messaging
{
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
        public void TryResolveTypeName_delegates_correctly()
        {
            // Arrange
            Type type = GetTypeFromReferencedAssembly();

            var sut = new TypeResolver(
                new DelegatingTypeNameResolvingStrategy(t => t.FullName),
                Mock.Of<ITypeResolvingStrategy>());

            // Act
            string actual = sut.TryResolveTypeName(type);

            // Assert
            actual.Should().Be(type.FullName);
        }

        [TestMethod]
        public void TryResolveType_delegates_correctly()
        {
            // Arrange
            Type type = GetTypeFromReferencedAssembly();

            var typeResolvingStrategy = new DelegatingTypeResolvingStrategy(
                typeName => typeName == type.FullName ? type : default);

            var sut = new TypeResolver(
                Mock.Of<ITypeNameResolvingStrategy>(), typeResolvingStrategy);

            // Act
            Type actual = sut.TryResolveType(type.FullName);

            // Assert
            actual.Should().Be(type);
        }
    }
}
