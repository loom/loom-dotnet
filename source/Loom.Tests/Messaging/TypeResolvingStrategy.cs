namespace Loom.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public class TypeResolvingStrategy : ITypeResolvingStrategy
    {
        private static readonly Lazy<IReadOnlyList<Type>> _types = new Lazy<IReadOnlyList<Type>>(GetAllTypes);

        private static IReadOnlyList<Type> GetAllTypes()
        {
            AppDomain appDomain = AppDomain.CurrentDomain;

            // TODO: Remove the code to bypass the damn error after it fixed.
            // https://github.com/microsoft/vstest/issues/2008
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
