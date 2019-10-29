namespace Loom.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    [Obsolete("This class is unstable. Use your own implementation of ITypeResolvingStrategy please.")]
    public class FullNameTypeResolvingStrategy : ITypeResolvingStrategy
    {
        private static readonly Lazy<IReadOnlyList<Type>> _types = new Lazy<IReadOnlyList<Type>>(GetAllTypes);

        private static IReadOnlyList<Type> GetAllTypes()
        {
            AppDomain appDomain = AppDomain.CurrentDomain;

            string filter = "Microsoft.VisualStudio.TraceDataCollector";
            IEnumerable<Assembly> assemblies =
                from assembly in appDomain.GetAssemblies()
                where assembly.FullName.StartsWith(filter, StringComparison.Ordinal) == false
                select assembly;

            IEnumerable<Type> query =
                from assembly in assemblies
                from type in assembly.GetTypes()
                select type;

            return query.ToList().AsReadOnly();
        }

        public Type TryResolveType(string typeName)
            => _types.Value.SingleOrDefault(t => t.FullName == typeName);
    }
}
