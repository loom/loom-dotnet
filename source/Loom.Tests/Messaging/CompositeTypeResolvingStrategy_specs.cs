using System;
using FluentAssertions;
using Loom.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Loom.Messaging;

[TestClass]
public class CompositeTypeResolvingStrategy_specs
{
    [TestMethod]
    public void sut_implements_ITypeResolvingStrategy()
    {
        Type tut = typeof(CompositeTypeResolvingStrategy);
        tut.Should().Implement<ITypeResolvingStrategy>();
    }

    [TestMethod, AutoData]
    public void sut_returns_first_result(
        [Frozen] string name,
        StrategyStub[] stubs)
    {
        CompositeTypeResolvingStrategy sut = new(strategies: stubs);
        Type? actual = sut.TryResolveType(name);
        actual.Should().Be(stubs[0].Type);
    }

    [TestMethod, AutoData]
    public void sut_skips_null(string name, Type type)
    {
        CompositeTypeResolvingStrategy sut = new(
            new StrategyStub(name, null),
            new StrategyStub(name, null),
            new StrategyStub(name, type));

        Type? actual = sut.TryResolveType(name);

        actual.Should().Be(type);
    }

    [TestMethod, AutoData]
    public void sut_returns_null_if_all_strategies_return_null(string name)
    {
        CompositeTypeResolvingStrategy sut = new(
            new StrategyStub(name, null),
            new StrategyStub(name, null),
            new StrategyStub(name, null));

        Type? actual = sut.TryResolveType(name);

        actual.Should().BeNull();
    }

    public sealed class StrategyStub : ITypeResolvingStrategy
    {
        public StrategyStub(string name, Type? type)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; }

        public Type? Type { get; }

        public Type? TryResolveType(string typeName)
            => typeName == Name ? Type : null;
    }
}
