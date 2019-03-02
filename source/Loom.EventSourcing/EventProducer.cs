namespace Loom.EventSourcing
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Reflection;

    public class EventProducer<T> : IEventProducer<T>
    {
        private readonly ImmutableDictionary<Type, MethodInfo> _producers;

        protected EventProducer()
        {
            const BindingFlags bindingFlags
                = BindingFlags.Instance
                | BindingFlags.Public
                | BindingFlags.NonPublic;

            IEnumerable<(Type commandPayloadType, MethodInfo producer)> query =
                from method in GetType().GetMethods(bindingFlags)

                where method.Name == "ProduceEventPayloads"
                where method.ReturnType == typeof(IEnumerable<object>)

                let parameters = method.GetParameters()
                where parameters.Length == 2

                let stateParam = parameters[0]
                where stateParam.ParameterType == typeof(T)

                let commandPayloadParam = parameters[1]
                let commandPayloadType = commandPayloadParam.ParameterType
                where commandPayloadType != typeof(object)

                select (commandPayloadType, producer: method);

            _producers = query.ToImmutableDictionary(
                keySelector: t => t.commandPayloadType,
                elementSelector: t => t.producer);
        }

        IEnumerable<object> IEventProducer<T>.ProduceEventPayloads(
            T state, object commandPayload)
        {
            Type commandPayloadType = commandPayload.GetType();
            switch (GetProducer(commandPayloadType))
            {
                case MethodInfo producer:
                    object[] arguments = new[] { state, commandPayload };
                    return (IEnumerable<object>)producer.Invoke(this, arguments);

                default:
                    string message = $"Cannot process the command of type {commandPayloadType}";
                    throw new InvalidOperationException(message);
            }
        }

        private MethodInfo GetProducer(Type commandPayloadType)
        {
            _producers.TryGetValue(commandPayloadType, out MethodInfo producer);
            return producer;
        }
    }
}
