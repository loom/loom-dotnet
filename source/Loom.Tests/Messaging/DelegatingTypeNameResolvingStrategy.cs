namespace Loom.Messaging
{
    using System;

    internal class DelegatingTypeNameResolvingStrategy :
        ITypeNameResolvingStrategy
    {
        private readonly Func<Type, string> _function;

        public DelegatingTypeNameResolvingStrategy(Func<Type, string> function)
        {
            _function = function;
        }

        public string ResolveTypeName(Type type)
        {
            return _function.Invoke(type);
        }
    }
}
