using System;

namespace Loom.Messaging
{
    public class FullNameTypeNameResolvingStrategy : ITypeNameResolvingStrategy
    {
        public string? TryResolveTypeName(Type type) => type switch
        {
            null => throw new ArgumentNullException(nameof(type)),
            _ => type.FullName,
        };
    }
}
