using System;

namespace Loom.Messaging
{
    internal class DelegatingTypeResolvingStrategy : ITypeResolvingStrategy
    {
        private readonly Func<string, Type?> _function;

        public DelegatingTypeResolvingStrategy(Func<string, Type?> function)
            => _function = function;

        public Type? TryResolveType(string typeName)
            => _function.Invoke(typeName);
    }
}
