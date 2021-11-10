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
        private readonly Lazy<Type[]> _interfaces;

        protected Headspring(Func<string, T> seedFactory, IEventStore eventStore)
            : base(seedFactory, eventStore)
        {
            _seedFactory = seedFactory;
            _eventCollector = eventStore;
            _interfaces = new(() => GetType().GetInterfaces());
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

            string streamId = (message.Data as dynamic).StreamId;
            Snapshot<T> snapshot = await RehydrateState(streamId, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            await StreamEvents(message, snapshot, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        }

        private Task StreamEvents(
            Message message,
            Snapshot<T> snapshot,
            CancellationToken cancellationToken)
        {
            object command = (object)(message.Data as dynamic).Payload;

            ImmutableArray<object> events = ProduceEvents(snapshot.State, command);

            ValidateEvents(events);

            return _eventCollector.CollectEvents(
                message.ProcessId,
                message.Initiator,
                predecessorId: message.Id,
                snapshot.StreamId,
                startVersion: snapshot.Version + 1,
                events,
                cancellationToken);
        }

        private ImmutableArray<object> ProduceEvents(T state, object command)
        {
            MethodInfo producer = GetEventProducer(command);
            var events = (IEnumerable<object>)producer.Invoke(this, new[] { state, command })!;
            return events.ToImmutableArray();
        }

        private static MethodInfo GetEventProducer(object payload)
        {
            return typeof(IEventProducer<,>)
                .MakeGenericType(typeof(T), payload.GetType())
                .GetMethod("ProduceEvents")!;
        }

        private void ValidateEvents(ImmutableArray<object> events)
        {
            foreach (Type eventType in events.Select(e => e.GetType()))
            {
                if (IsEventAcceptable(eventType) == false)
                {
                    throw new InvalidOperationException($"Cannot handle the event of {eventType}.");
                }
            }
        }

        private bool IsEventAcceptable(Type eventType)
        {
            Type template = typeof(IEventHandler<,>);
            Type handlerType = template.MakeGenericType(typeof(T), eventType);
            return _interfaces.Value.Contains(handlerType);
        }
    }
}
