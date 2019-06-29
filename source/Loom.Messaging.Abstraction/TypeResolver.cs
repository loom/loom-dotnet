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

        public string ResolveTypeName(Type type)
            => _typeNameResolvingStrategy.ResolveTypeName(type);

        public string ResolveTypeName<T>() => ResolveTypeName(typeof(T));

        // TODO: Change return type to Type?.
        public Type TryResolveType(string typeName)
            => _typeResolvingStrategy.TryResolveType(typeName);
    }
}
