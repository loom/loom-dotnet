namespace Loom.Messaging
{
    using System;
    using System.Collections.Generic;

    public interface ITypeResolvingStrategy
    {
        // TODO: Change return type to Type?.
        Type TryResolveType(string typeName);
    }
}
