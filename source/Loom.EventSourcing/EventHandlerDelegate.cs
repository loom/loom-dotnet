namespace Loom.EventSourcing
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Reflection;

    public sealed class EventHandlerDelegate<T> : IEventHandler<T>
    {
        private readonly object _handler;
        private readonly ImmutableDictionary<Type, MethodInfo> _functions;

        public EventHandlerDelegate(object handler)
        {
            // TODO: Remove the following guard clause after apply C# 8.0.
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));

            const BindingFlags bindingFlags
                = BindingFlags.Static
                | BindingFlags.Instance
                | BindingFlags.Public;

            IEnumerable<(Type eventType, MethodInfo function)> query =
                from method in _handler.GetType().GetMethods(bindingFlags)

                where method.Name == "HandleEvent"
                where method.ReturnType == typeof(T)

                let parameters = method.GetParameters()
                where parameters.Length == 2

                let stateParam = parameters[0]
                where stateParam.ParameterType == typeof(T)

                let eventType = parameters[1].ParameterType

                select (eventType, function: method);

            _functions = query.ToImmutableDictionary(
                keySelector: t => t.eventType,
                elementSelector: t => t.function);
        }

        public T HandleEvents(T state, IEnumerable<object> events)
            => events.Aggregate(state, Handle);

        private T Handle(T state, object @event)
        {
            Type eventType = @event.GetType();
            switch (_functions.TryGetValue(eventType, out MethodInfo function))
            {
                case true:
                    object[] arguments = new[] { state, @event };
                    return (T)function.Invoke(_handler, arguments);

                default:
                    string message = $"Cannot handle the event of type {eventType}.";
                    throw new InvalidOperationException(message);
            }
        }
    }
}
