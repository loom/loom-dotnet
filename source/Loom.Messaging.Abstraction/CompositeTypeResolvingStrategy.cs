using System;
using System.Collections.Immutable;
using System.Linq;

namespace Loom.Messaging
{
    public sealed class CompositeTypeResolvingStrategy : ITypeResolvingStrategy
    {
        private readonly ImmutableArray<ITypeResolvingStrategy> _strategies;

        public CompositeTypeResolvingStrategy(
            params ITypeResolvingStrategy[] strategies)
        {
            _strategies = ImmutableArray.CreateRange(strategies);
        }

        public Type? TryResolveType(string typeName)
        {
            return _strategies
                .Select(strategy => strategy.TryResolveType(typeName))
                .FirstOrDefault(result => result != null);
        }
    }
}
