using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Loom.Messaging;

namespace Loom.EventSourcing
{
    public abstract class Headspring<T> : Rehydrator<T>, IMessageHandler
        where T : class?
    {
        private readonly Func<string, T> _seedFactory;
        private readonly IEventCollector _eventCollector;

        protected Headspring(Func<string, T> seedFactory, IEventStore eventStore)
            : base(seedFactory, eventStore)
        {
            _seedFactory = seedFactory;
            _eventCollector = eventStore;
        }

        public bool CanHandle(Message message)
        {
            IEnumerable<Type> query =
                from t in GetType().GetInterfaces()
                where t.IsGenericType
                where t.GetGenericTypeDefinition() == typeof(IEventProducer<,>)
                where t.GenericTypeArguments[0] == typeof(T)
                select typeof(StreamCommand<>).MakeGenericType(t.GenericTypeArguments[1]);

            return query.Any(t => t == message.Data.GetType());
        }

        public async Task Handle(Message message, CancellationToken cancellationToken = default)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (CanHandle(message) == false)
            {
                throw new InvalidOperationException($"Cannot handle message of {message.Data.GetType()}");
            }

            dynamic data = message.Data;
            string streamId = data.StreamId;

            (long version, T state) = await
                RehydrateState(streamId, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);

            await _eventCollector.CollectEvents(
                message.ProcessId,
                message.Initiator,
                predecessorId: message.Id,
                streamId,
                startVersion: version + 1,
                events: ProduceEvents(state, command: (object)data.Payload),
                cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        }

        private ImmutableArray<object> ProduceEvents(T state, object command)
        {
            MethodInfo producer = GetEventProducer(command);
            ImmutableArray<object> events = ProduceEvents(producer, state, command);
            ValidateEvents(events);
            return events;
        }

        private static MethodInfo GetEventProducer(object payload)
        {
            return typeof(IEventProducer<,>)
                .MakeGenericType(typeof(T), payload.GetType())
                .GetMethod("ProduceEvents")!;
        }

        private ImmutableArray<object> ProduceEvents(MethodInfo producer, T state, object command)
        {
            var events = (IEnumerable<object>)producer.Invoke(this, new[] { state, command })!;
            return events.ToImmutableArray();
        }

        private void ValidateEvents(ImmutableArray<object> events)
        {
            Type[] interfaces = GetType().GetInterfaces();
            foreach (Type eventType in events.Select(e => e.GetType()))
            {
                Type handlerType = typeof(IEventHandler<,>).MakeGenericType(typeof(T), eventType);
                if (interfaces.Contains(handlerType) == false)
                {
                    throw new InvalidOperationException($"Cannot handle the event of {eventType}.");
                }
            }
        }
    }
}
