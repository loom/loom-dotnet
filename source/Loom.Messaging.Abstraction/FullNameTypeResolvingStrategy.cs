namespace Loom.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class FullNameTypeResolvingStrategy : ITypeResolvingStrategy
    {
        public Type TryResolveType(IEnumerable<Type> types, string typeName)
            => types.SingleOrDefault(t => t.FullName == typeName);
    }
}
