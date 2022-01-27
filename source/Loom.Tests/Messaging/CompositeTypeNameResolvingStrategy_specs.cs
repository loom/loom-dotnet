using System;
using FluentAssertions;
using Loom.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Loom.Messaging;

[TestClass]
public class CompositeTypeNameResolvingStrategy_specs
{
    [TestMethod]
    public void sut_implements_ITypeNameResolvingStrategy()
    {
        Type tut = typeof(CompositeTypeNameResolvingStrategy);
        tut.Should().Implement<ITypeNameResolvingStrategy>();
    }

    [TestMethod, AutoData]
    public void sut_returns_first_result(
        [Frozen] Type type,
        StrategyStub[] stubs)
    {
        CompositeTypeNameResolvingStrategy sut = new(strategies: stubs);
        string? actual = sut.TryResolveTypeName(type);
        actual.Should().Be(stubs[0].Name);
    }

    [TestMethod, AutoData]
    public void sut_skips_null(Type type, string name)
    {
        CompositeTypeNameResolvingStrategy sut = new(
            new StrategyStub(type, null),
            new StrategyStub(type, null),
            new StrategyStub(type, name));

        string? actual = sut.TryResolveTypeName(type);

        actual.Should().Be(name);
    }

    [TestMethod, AutoData]
    public void sut_returns_null_if_all_strategies_return_null(Type type)
    {
        CompositeTypeNameResolvingStrategy sut = new(
            new StrategyStub(type, null),
            new StrategyStub(type, null),
            new StrategyStub(type, null));

        string? actual = sut.TryResolveTypeName(type);

        actual.Should().BeNull();
    }

    public sealed class StrategyStub : ITypeNameResolvingStrategy
    {
        public StrategyStub(Type type, string? name)
        {
            Type = type;
            Name = name;
        }

        public Type Type { get; }

        public string? Name { get; }

        public string? TryResolveTypeName(Type type)
            => type == Type ? Name : null;
    }
}
