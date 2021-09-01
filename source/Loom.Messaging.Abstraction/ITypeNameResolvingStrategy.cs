namespace Loom.Messaging
{
    using System;

    public interface ITypeNameResolvingStrategy
    {
        string? TryResolveTypeName(Type type);
    }
}
