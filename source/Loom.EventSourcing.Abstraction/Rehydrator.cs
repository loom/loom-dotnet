using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Loom.EventSourcing
{
    public abstract class Rehydrator<T>
    {
        private readonly Func<string, T> _seedFactory;
        private readonly IEventReader _eventReader;

        protected Rehydrator(Func<string, T> seedFactory, IEventReader eventReader)
        {
            _seedFactory = seedFactory;
            _eventReader = eventReader;
        }

        public async Task<Snapshot<T>> RehydrateState(
            string streamId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<object> events = await QueryEvents(streamId, cancellationToken).ConfigureAwait(false);
            return FoldEvents(streamId, events);
        }

        private async Task<IReadOnlyCollection<object>> QueryEvents(
            string streamId,
            CancellationToken cancellationToken)
        {
            IEnumerable<object> events = await _eventReader
                .QueryEvents(streamId, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);

            return events.ToList();
        }

        private Snapshot<T> FoldEvents(
            string streamId,
            IReadOnlyCollection<object> events)
        {
            T seed = _seedFactory.Invoke(streamId);
            T state = events.Aggregate(seed, HandleEvent);
            return new(Version: events.Count, state);
        }

        private T HandleEvent(T state, object pastEvent)
        {
            MethodInfo handle = typeof(IEventHandler<,>)
                .MakeGenericType(typeof(T), pastEvent.GetType())
                .GetMethod("HandleEvent")!;

            return (T)handle.Invoke(this, new[] { state, pastEvent })!;
        }
    }
}
