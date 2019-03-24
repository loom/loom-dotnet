namespace Loom.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

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

            IEnumerable<Type> query =
                from assembly in appDomain.GetAssemblies()
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
