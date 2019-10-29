namespace Loom.EventSourcing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public sealed class EventProducerDelegate<T> : IEventProducer<T>
    {
        private readonly object _producer;
        private readonly IReadOnlyDictionary<Type, MethodInfo> _functions;

        public EventProducerDelegate(object producer)
        {
            _producer = producer ?? throw new ArgumentNullException(nameof(producer));

            const BindingFlags bindingFlags
                = BindingFlags.Static
                | BindingFlags.Instance
                | BindingFlags.Public;

            IEnumerable<(Type commandType, MethodInfo function)> query =
                from method in _producer.GetType().GetMethods(bindingFlags)

                where method.Name == "ProduceEvents"
                where method.ReturnType == typeof(IEnumerable<object>)

                let parameters = method.GetParameters()
                where parameters.Length == 2

                let stateParam = parameters[0]
                where stateParam.ParameterType == typeof(T)

                let commandType = parameters[1].ParameterType

                select (commandType, function: method);

            _functions = query.ToDictionary(
                keySelector: t => t.commandType,
                elementSelector: t => t.function);
        }

        public IEnumerable<object> ProduceEvents(T state, object command)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            Type commandType = command.GetType();
            return _functions.TryGetValue(commandType, out MethodInfo function) switch
            {
                true => (IEnumerable<object>)function.Invoke(_producer, new[] { state, command }),
                _ => throw new InvalidOperationException($"Cannot execute the command of type {commandType}."),
            };
        }
    }
}
