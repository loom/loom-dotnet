using System;

namespace Loom.Messaging
{
    public interface ITypeResolvingStrategy
    {
        Type? TryResolveType(string typeName);
    }
}
