namespace Loom.Messaging
{
    using System;

    public class FullNameTypeNameResolvingStrategy : ITypeNameResolvingStrategy
    {
        public string ResolveTypeName(Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return type.FullName;
        }
    }
}
