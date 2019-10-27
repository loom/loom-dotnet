namespace Loom.Messaging
{
    using System;
    using System.Collections.Concurrent;

    public sealed class CachingTypeResolvingStrategy : ITypeResolvingStrategy
    {
        private readonly ITypeResolvingStrategy _strategy;
        private readonly ConcurrentDictionary<string, Type?> _cache;

        public CachingTypeResolvingStrategy(ITypeResolvingStrategy strategy)
        {
            _strategy = strategy;
            _cache = new ConcurrentDictionary<string, Type?>();
        }

        public Type? TryResolveType(string typeName)
        {
            return _cache.GetOrAdd(typeName, Relay);
        }

        private Type? Relay(string typeName)
        {
            try
            {
                return _strategy.TryResolveType(typeName);
            }
            catch
            {
                return null;
            }
        }
    }
}
