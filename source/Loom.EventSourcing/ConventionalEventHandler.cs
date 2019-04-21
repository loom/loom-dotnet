namespace Loom.EventSourcing
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Reflection;

    public abstract class ConventionalEventHandler<T> : IEventHandler<T>
    {
        private readonly ImmutableDictionary<Type, MethodInfo> _handlers;

        protected ConventionalEventHandler()
        {
            const BindingFlags bindingFlags
                = BindingFlags.Instance
                | BindingFlags.Public
                | BindingFlags.NonPublic;

            IEnumerable<(Type eventType, MethodInfo handler)> query =
                from method in GetType().GetMethods(bindingFlags)

                where method.Name == "HandleEvent"
                where method.ReturnType == typeof(T)

                let parameters = method.GetParameters()
                where parameters.Length == 2

                let stateParam = parameters[0]
                where stateParam.ParameterType == typeof(T)

                let eventType = parameters[1].ParameterType
                where eventType != typeof(IEnumerable<object>)

                select (eventType, handler: method);

            _handlers = query.ToImmutableDictionary(
                keySelector: t => t.eventType,
                elementSelector: t => t.handler);
        }

        private T Handle(T state, object @event)
        {
            Type eventType = @event.GetType();
            _handlers.TryGetValue(eventType, out MethodInfo handler);
            switch (handler)
            {
                case MethodInfo _:
                    object[] arguments = new[] { state, @event };
                    return (T)handler.Invoke(this, arguments);

                default:
                    string message = $"Cannot handle the event of type {eventType}.";
                    throw new InvalidOperationException(message);
            }
        }

        T IEventHandler<T>.HandleEvents(T state, IEnumerable<object> events)
            => events.Aggregate(state, Handle);
    }
}
