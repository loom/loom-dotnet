using System;

namespace Loom.Messaging
{
    internal class DelegatingTypeNameResolvingStrategy :
        ITypeNameResolvingStrategy
    {
        private readonly Func<Type, string> _function;

        public DelegatingTypeNameResolvingStrategy(Func<Type, string> function)
        {
            _function = function;
        }

        public string TryResolveTypeName(Type type)
        {
            return _function.Invoke(type);
        }
    }
}
