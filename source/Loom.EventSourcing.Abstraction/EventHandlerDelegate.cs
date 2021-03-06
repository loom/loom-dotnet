﻿namespace Loom.EventSourcing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public sealed class EventHandlerDelegate<T> : IEventHandler<T>
    {
        private readonly object _handler;
        private readonly IReadOnlyDictionary<Type, MethodInfo> _functions;

        public EventHandlerDelegate(object handler)
        {
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

            _functions = query.ToDictionary(
                keySelector: t => t.eventType,
                elementSelector: t => t.function);
        }

        public T HandleEvents(T state, IEnumerable<object> events)
            => events.Aggregate(state, Handle);

        private T Handle(T state, object raisedEvent)
        {
            Type eventType = raisedEvent.GetType();
            return _functions.TryGetValue(eventType, out MethodInfo function) switch
            {
                true => (T)function.Invoke(_handler, new[] { state, raisedEvent }),
                _ => throw new InvalidOperationException($"Cannot handle the event of type {eventType}."),
            };
        }
    }
}
