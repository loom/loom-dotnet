namespace Loom.Messaging
{
    using System;
    using System.Collections.Generic;

    internal class DelegatingTypeResolvingStrategy : ITypeResolvingStrategy
    {
        private readonly Func<IEnumerable<Type>, string, Type> _function;

        public DelegatingTypeResolvingStrategy(
            Func<IEnumerable<Type>, string, Type> function)
        {
            _function = function;
        }

        public Type TryResolveType(IEnumerable<Type> types, string typeName)
        {
            return _function.Invoke(types, typeName);
        }
    }
}
