using System;
using FluentAssertions;
using Loom.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Loom.Messaging
{
    [TestClass]
    public class CachingTypeResolvingStrategy_specs
    {
        [TestMethod]
        public void sut_implements_ITypeResolvingStrategy()
        {
            typeof(CachingTypeResolvingStrategy).Should().Implement<ITypeResolvingStrategy>();
        }

        [TestMethod]
        [InlineAutoData(arguments: new object[] { null })]
        [InlineAutoData(typeof(MessageData1))]
        public void sut_relays_correctly(Type type, string typeName)
        {
            ITypeResolvingStrategy strategy = Mock.Of<ITypeResolvingStrategy>();
            Mock.Get(strategy).Setup(x => x.TryResolveType(typeName)).Returns(type);
            var sut = new CachingTypeResolvingStrategy(strategy);

            Type actual = sut.TryResolveType(typeName);

            actual.Should().Be(type);
        }

        [TestMethod, AutoData]
        public void sut_caches_result(string typeName)
        {
            ITypeResolvingStrategy strategy = Mock.Of<ITypeResolvingStrategy>();
            Type type = typeof(MessageData1);
            Mock.Get(strategy).Setup(x => x.TryResolveType(typeName)).Returns(type);
            var sut = new CachingTypeResolvingStrategy(strategy);

            sut.TryResolveType(typeName);
            Type actual = sut.TryResolveType(typeName);

            actual.Should().Be(type);
            Mock.Get(strategy).Verify(x => x.TryResolveType(It.IsAny<string>()), Times.Once());
        }

        [TestMethod, AutoData]
        public void sut_caches_null_if_strategy_fails(string typeName)
        {
            ITypeResolvingStrategy strategy = Mock.Of<ITypeResolvingStrategy>();
            Mock.Get(strategy).Setup(x => x.TryResolveType(typeName)).Throws<InvalidOperationException>();
            var sut = new CachingTypeResolvingStrategy(strategy);

            sut.TryResolveType(typeName);
            Type actual = sut.TryResolveType(typeName);

            actual.Should().BeNull();
            Mock.Get(strategy).Verify(x => x.TryResolveType(It.IsAny<string>()), Times.Once());
        }
    }
}
