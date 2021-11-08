using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Loom.EventSourcing
{
    [Obsolete("This class will be replaced with new framework.")]
    public class SnapshottedStateRehydrator<T> : IStateRehydrator<T>
        where T : class, IVersioned
    {
        private readonly Func<string, T> _seedFactory;
        private readonly ISnapshotReader<T> _snapshotReader;
        private readonly IEventReader _eventReader;
        private readonly IEventHandler<T> _eventHandler;

        public SnapshottedStateRehydrator(Func<string, T> seedFactory,
                                          ISnapshotReader<T> snapshotReader,
                                          IEventReader eventReader,
                                          IEventHandler<T> eventHandler)
        {
            _seedFactory = seedFactory;
            _snapshotReader = snapshotReader;
            _eventReader = eventReader;
            _eventHandler = eventHandler;
        }

        public async Task<T?> TryRehydrateState(string streamId)
        {
            return (await _snapshotReader.TryRestoreSnapshot(streamId).ConfigureAwait(continueOnCapturedContext: false)) switch
            {
                T snapshot => await Rehydrate(streamId, snapshot).ConfigureAwait(continueOnCapturedContext: false),
                _ => await TryRehydrate(streamId).ConfigureAwait(continueOnCapturedContext: false),
            };
        }

        private async Task<T> Rehydrate(string streamId, T snapshot)
        {
            long fromVersion = snapshot.Version + 1;
            return (await _eventReader.QueryEvents(streamId, fromVersion).ConfigureAwait(continueOnCapturedContext: false)) switch
            {
                var events when events.Any() == false => snapshot,
                var events => FoldLeft(snapshot, events),
            };
        }

        private async Task<T?> TryRehydrate(string streamId)
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

        private T FoldLeft(T seed, IEnumerable<object> events) => _eventHandler.HandleEvents(seed, events);
    }
}
