using System;
using System.Collections.Generic;
using System.Linq;

namespace Loom.Messaging
{
    public class TypeResolvingStrategy : ITypeResolvingStrategy
    {
        private static readonly Lazy<IReadOnlyList<Type>> _types = new(GetAllTypes);

        private static IReadOnlyList<Type> GetAllTypes()
        {
            IEnumerable<Type> query =
                from assembly in AppDomain.CurrentDomain.GetAssemblies()
                from type in assembly.GetTypes()
                select type;

            return query.ToList().AsReadOnly();
        }

        public Type TryResolveType(string typeName)
            => _types.Value.SingleOrDefault(t => t.FullName == typeName);
    }
}
