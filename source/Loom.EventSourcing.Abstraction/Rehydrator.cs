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

        public async Task<Snapshot<T>> Rehydrate(
            string streamId,
            CancellationToken cancellationToken = default)
        {
            var events = new List<object>(await _eventReader.QueryEvents(streamId).ConfigureAwait(continueOnCapturedContext: false));
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
