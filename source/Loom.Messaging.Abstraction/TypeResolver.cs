namespace Loom.Messaging
{
    using System;

    public sealed class TypeResolver
    {
        private readonly ITypeNameResolvingStrategy _typeNameResolvingStrategy;
        private readonly ITypeResolvingStrategy _typeResolvingStrategy;

        public TypeResolver(
            ITypeNameResolvingStrategy typeNameResolvingStrategy,
            ITypeResolvingStrategy typeResolvingStrategy)
        {
            _typeNameResolvingStrategy = typeNameResolvingStrategy;
            _typeResolvingStrategy = typeResolvingStrategy;
        }

        public string? TryResolveTypeName(Type type)
            => _typeNameResolvingStrategy.TryResolveTypeName(type);

        public string? TryResolveTypeName<T>() => TryResolveTypeName(typeof(T));

        public Type? TryResolveType(string typeName)
            => _typeResolvingStrategy.TryResolveType(typeName);
    }
}
