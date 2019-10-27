namespace Loom.Messaging
{
    using System;

    public interface ITypeResolvingStrategy
    {
        Type? TryResolveType(string typeName);
    }
}
