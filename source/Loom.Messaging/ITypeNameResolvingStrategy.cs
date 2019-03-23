namespace Loom.Messaging
{
    using System;

    public interface ITypeNameResolvingStrategy
    {
        string ResolveTypeName(Type type);
    }
}
