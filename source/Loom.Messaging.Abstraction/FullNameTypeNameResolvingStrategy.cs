namespace Loom.Messaging
{
    using System;

    public class FullNameTypeNameResolvingStrategy : ITypeNameResolvingStrategy
    {
// TODO: Use nullable-reference in C# 8.0 and remove the following preprocessor.
#pragma warning disable CA1062 // Validate arguments of public methods
        public string ResolveTypeName(Type type) => type.FullName;
#pragma warning restore CA1062 // Validate arguments of public methods
    }
}
