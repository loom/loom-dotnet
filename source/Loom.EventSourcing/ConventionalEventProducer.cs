namespace Loom.EventSourcing
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Reflection;

    public abstract class ConventionalEventProducer<T> : IEventProducer<T>
    {
        private readonly ImmutableDictionary<Type, MethodInfo> _producers;

        protected ConventionalEventProducer()
        {
            const BindingFlags bindingFlags
                = BindingFlags.Instance
                | BindingFlags.Public
                | BindingFlags.NonPublic;

            IEnumerable<(Type commandType, MethodInfo producer)> query =
                from method in GetType().GetMethods(bindingFlags)

                where method.Name == "ProduceEvents"
                where method.ReturnType == typeof(IEnumerable<object>)

                let parameters = method.GetParameters()
                where parameters.Length == 2

                let stateParam = parameters[0]
                where stateParam.ParameterType == typeof(T)

                let commandType = parameters[1].ParameterType
                where commandType != typeof(object)

                select (commandType, producer: method);

            _producers = query.ToImmutableDictionary(
                keySelector: t => t.commandType,
                elementSelector: t => t.producer);
        }

#pragma warning disable CA1033 // Interface methods should be callable by child types

        // This method is not for child types but for the framework.
        IEnumerable<object> IEventProducer<T>.ProduceEvents(
            T state, object command)
        {
            Type commandType = command.GetType();
            switch (GetProducer(commandType))
            {
                case MethodInfo producer:
                    object[] arguments = new[] { state, command };
                    return (IEnumerable<object>)producer.Invoke(this, arguments);

                default:
                    string message = $"Cannot execute the command of type {commandType}.";
                    throw new InvalidOperationException(message);
            }
        }

#pragma warning restore CA1033 // Interface methods should be callable by child types

        private MethodInfo GetProducer(Type commandType)
        {
            _producers.TryGetValue(commandType, out MethodInfo producer);
            return producer;
        }
    }
}
