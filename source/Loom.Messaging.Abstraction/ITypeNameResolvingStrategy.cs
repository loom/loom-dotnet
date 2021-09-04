using System;

namespace Loom.Messaging
{
    public interface ITypeNameResolvingStrategy
    {
        string? TryResolveTypeName(Type type);
    }
}
