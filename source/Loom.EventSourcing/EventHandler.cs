namespace Loom.EventSourcing
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Reflection;

    public abstract class EventHandler<T> : IEventHandler<T>
    {
        private readonly ImmutableDictionary<Type, MethodInfo> _handlers;

        protected EventHandler()
        {
            const BindingFlags bindingFlags
                = BindingFlags.Instance
                | BindingFlags.Public
                | BindingFlags.NonPublic;

            IEnumerable<(Type eventPayloadType, MethodInfo handler)> query =
                from method in GetType().GetMethods(bindingFlags)

                where method.Name == "Handle"
                where method.ReturnType == typeof(T)

                let parameters = method.GetParameters()
                where parameters.Length == 2

                let stateParam = parameters[0]
                where stateParam.ParameterType == typeof(T)

                let eventPayloadParam = parameters[1]
                let eventPayloadType = eventPayloadParam.ParameterType
                where eventPayloadType != typeof(object)

                select (eventPayloadType, handler: method);

            _handlers = query.ToImmutableDictionary(
                keySelector: t => t.eventPayloadType,
                elementSelector: t => t.handler);
        }

        T IEventHandler<T>.Handle(T state, object eventPayload)
        {
            Type eventPayloadType = eventPayload.GetType();
            _handlers.TryGetValue(eventPayloadType, out MethodInfo handler);
            switch (handler)
            {
                case MethodInfo _:
                    object[] arguments = new[] { state, eventPayload };
                    return (T)handler.Invoke(this, arguments);

                default:
                    string message = $"Cannot handle the event of type {eventPayloadType}";
                    throw new InvalidOperationException(message);
            }
        }
    }
}
