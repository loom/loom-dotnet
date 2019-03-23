namespace Loom.Messaging
{
    using System;

    public class FullNameTypeNameResolvingStrategy : ITypeNameResolvingStrategy
    {
        public string ResolveTypeName(Type type) => type.FullName;
    }
}
