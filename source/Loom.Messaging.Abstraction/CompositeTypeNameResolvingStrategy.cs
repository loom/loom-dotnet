using System;
using System.Collections.Immutable;
using System.Linq;

namespace Loom.Messaging
{
    public sealed class CompositeTypeNameResolvingStrategy :
        ITypeNameResolvingStrategy
    {
        private readonly ImmutableArray<ITypeNameResolvingStrategy> _strategies;

        public CompositeTypeNameResolvingStrategy(
            params ITypeNameResolvingStrategy[] strategies)
        {
            _strategies = ImmutableArray.CreateRange(strategies);
        }

        public string? TryResolveTypeName(Type type)
        {
            return _strategies
                .Select(strategy => strategy.TryResolveTypeName(type))
                .FirstOrDefault(result => result != null);
        }
    }
}
