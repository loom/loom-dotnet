using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Loom.EventSourcing
{
    public class SimpleStateRehydrator<T> : IStateRehydrator<T>
        where T : class
    {
        private readonly Func<string, T> _seedFactory;
        private readonly IEventReader _eventReader;
        private readonly IEventHandler<T> _eventHandler;

        public SimpleStateRehydrator(Func<string, T> seedFactory,
                                     IEventReader eventReader,
                                     IEventHandler<T> eventHandler)
        {
            _seedFactory = seedFactory;
            _eventReader = eventReader;
            _eventHandler = eventHandler;
        }

        public async Task<T?> TryRehydrateState(string streamId)
        {
            return (await _eventReader.QueryEvents(streamId, fromVersion: 1).ConfigureAwait(continueOnCapturedContext: false)) switch
            {
                var events when events.Any() == false => default,
                var events => FoldLeft(_seedFactory.Invoke(streamId), events),
            };
        }

        public async Task<T?> TryRehydrateStateAt(string streamId, long version)
        {
            return (await _eventReader.QueryEvents(streamId, fromVersion: 1).ConfigureAwait(continueOnCapturedContext: false)) switch
            {
                var events when events.Any() == false => default,
                var events when events.Count() < version => throw new InvalidOperationException($"State of the specified version({version}) does not exists."),
                var events => FoldLeft(_seedFactory.Invoke(streamId), events.TakeWhile((_, index) => index < version)),
            };
        }

        private T FoldLeft(T seed, IEnumerable<object> events)
            => _eventHandler.HandleEvents(seed, events);
    }
}
