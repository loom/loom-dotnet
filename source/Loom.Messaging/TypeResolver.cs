namespace Loom.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Reflection;

    public sealed class TypeResolver
    {
        private static readonly Lazy<ImmutableArray<Type>> _types;

        private readonly ITypeNameResolvingStrategy _typeNameResolvingStrategy;
        private readonly ITypeResolvingStrategy _typeResolvingStrategy;

        static TypeResolver()
        {
            _types = new Lazy<ImmutableArray<Type>>(GetAllTypes);
        }

        public TypeResolver(
            ITypeNameResolvingStrategy typeNameResolvingStrategy,
            ITypeResolvingStrategy typeResolvingStrategy)
        {
            _typeNameResolvingStrategy = typeNameResolvingStrategy;
            _typeResolvingStrategy = typeResolvingStrategy;
        }

        private static ImmutableArray<Type> GetAllTypes()
        {
            AppDomain appDomain = AppDomain.CurrentDomain;

            // TODO: Remove the code to bypass the damn error after it fixed.
            // https://github.com/microsoft/vstest/issues/2008
            string filter = "Microsoft.VisualStudio.TraceDataCollector";
            IEnumerable<Assembly> assemblies =
                from assembly in appDomain.GetAssemblies()
                where assembly.FullName.StartsWith(filter) == false
                select assembly;

            IEnumerable<Type> query =
                from assembly in assemblies
                from type in assembly.GetTypes()
                select type;

            return query.ToImmutableArray();
        }

        public string ResolveTypeName(Type type)
            => _typeNameResolvingStrategy.ResolveTypeName(type);

        public string ResolveTypeName<T>() => ResolveTypeName(typeof(T));

        // TODO: Change return type to Type?.
        public Type TryResolveType(string typeName)
            => _typeResolvingStrategy.TryResolveType(_types.Value, typeName);
    }
}
