using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Loom.EventSourcing
{
    public class SnapshottedStateRehydrator<T> : IStateRehydrator<T>
        where T : class, IVersioned
    {
        private readonly Func<Guid, T> _seedFactory;
        private readonly ISnapshotReader<T> _snapshotReader;
        private readonly IEventReader _eventReader;
        private readonly IEventHandler<T> _eventHandler;

        public SnapshottedStateRehydrator(Func<Guid, T> seedFactory,
                                          ISnapshotReader<T> snapshotReader,
                                          IEventReader eventReader,
                                          IEventHandler<T> eventHandler)
        {
            _seedFactory = seedFactory;
            _snapshotReader = snapshotReader;
            _eventReader = eventReader;
            _eventHandler = eventHandler;
        }

        public async Task<T?> TryRehydrateState(Guid streamId)
        {
            return (await _snapshotReader.TryRestoreSnapshot(streamId).ConfigureAwait(continueOnCapturedContext: false)) switch
            {
                T snapshot => await Rehydrate(streamId, snapshot).ConfigureAwait(continueOnCapturedContext: false),
                _ => await TryRehydrate(streamId).ConfigureAwait(continueOnCapturedContext: false),
            };
        }

        private async Task<T> Rehydrate(Guid streamId, T snapshot)
        {
            long fromVersion = snapshot.Version + 1;
            return (await _eventReader.QueryEvents(streamId, fromVersion).ConfigureAwait(continueOnCapturedContext: false)) switch
            {
                var events when events.Any() == false => snapshot,
                var events => FoldLeft(snapshot, events),
            };
        }

        private async Task<T?> TryRehydrate(Guid streamId)
        {
            return (await _eventReader.QueryEvents(streamId, fromVersion: 1).ConfigureAwait(continueOnCapturedContext: false)) switch
            {
                var events when events.Any() == false => default,
                var events => FoldLeft(_seedFactory.Invoke(streamId), events),
            };
        }

        public async Task<T?> TryRehydrateStateAt(Guid streamId, long version)
        {
            return (await _eventReader.QueryEvents(streamId, fromVersion: 1).ConfigureAwait(continueOnCapturedContext: false)) switch
            {
                var events when events.Any() == false => default,
                var events when events.Count() < version => throw new InvalidOperationException($"State of the specified version({version}) does not exists."),
                var events => FoldLeft(_seedFactory.Invoke(streamId), events.TakeWhile((_, index) => index < version)),
            };
        }

        private T FoldLeft(T seed, IEnumerable<object> events) => _eventHandler.HandleEvents(seed, events);
    }
}
