namespace Loom.EventSourcing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class SimpleStateRehydrator<T> : IStateRehydrator<T>
    {
        private readonly Func<Guid, T> _seedFactory;
        private readonly IEventReader _eventReader;
        private readonly IEventHandler<T> _eventHandler;

        public SimpleStateRehydrator(Func<Guid, T> seedFactory,
                                     IEventReader eventReader,
                                     IEventHandler<T> eventHandler)
        {
            _seedFactory = seedFactory;
            _eventReader = eventReader;
            _eventHandler = eventHandler;
        }

        public async Task<T> TryRehydrateState(Guid streamId)
        {
            switch (await _eventReader.QueryEvents(streamId, fromVersion: 1).ConfigureAwait(continueOnCapturedContext: false))
            {
                case var events when events.Any() == false: return default;
                case var events: return FoldLeft(_seedFactory.Invoke(streamId), events);
            }
        }

        public async Task<T> TryRehydrateStateAt(Guid streamId, long version)
        {
            switch (await _eventReader.QueryEvents(streamId, fromVersion: 1).ConfigureAwait(continueOnCapturedContext: false))
            {
                case var events when events.Any() == false: return default;
                case var events when events.Count() < version: throw new InvalidOperationException($"State of the specified version({version}) does not exists.");
                case var events: return FoldLeft(_seedFactory.Invoke(streamId), events.TakeWhile((_, index) => index < version));
            }
        }

        private T FoldLeft(T seed, IEnumerable<object> events)
            => _eventHandler.HandleEvents(seed, events);
    }
}
