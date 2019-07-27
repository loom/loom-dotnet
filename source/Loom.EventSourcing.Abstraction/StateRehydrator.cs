namespace Loom.EventSourcing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    [Obsolete("Use class SimpleStateRehydrator<T> or class SnapshottedStateRehydrator<T> instead.")]
    public class StateRehydrator<T> : IStateRehydrator<T>
        where T : IVersioned, new()
    {
        private readonly ISnapshotReader<T> _snapshotReader;
        private readonly IEventReader _eventReader;
        private readonly IEventHandler<T> _eventHandler;

        public StateRehydrator(ISnapshotReader<T> snapshotReader,
                               IEventReader eventReader,
                               IEventHandler<T> eventHandler)
        {
            _snapshotReader = snapshotReader;
            _eventReader = eventReader;
            _eventHandler = eventHandler;
        }

        public StateRehydrator(IEventReader eventReader,
                               IEventHandler<T> eventHandler)
        {
            _snapshotReader = DefaultSnapshotReader.Instance;
            _eventReader = eventReader;
            _eventHandler = eventHandler;
        }

        public async Task<T> TryRehydrateState(Guid streamId)
        {
            switch (await _snapshotReader.TryRestoreSnapshot(streamId).ConfigureAwait(continueOnCapturedContext: false))
            {
                case T snapshot: return await Rehydrate(streamId, snapshot).ConfigureAwait(continueOnCapturedContext: false);
                default: return await TryRehydrate(streamId).ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        private async Task<T> Rehydrate(Guid streamId, T snapshot)
        {
            long fromVersion = snapshot.Version + 1;
            switch (await _eventReader.QueryEvents(streamId, fromVersion).ConfigureAwait(continueOnCapturedContext: false))
            {
                case var events when events.Any() == false: return snapshot;
                case var events: return FoldLeft(snapshot, events);
            }
        }

        private async Task<T> TryRehydrate(Guid streamId)
        {
            switch (await _eventReader.QueryEvents(streamId, fromVersion: 1).ConfigureAwait(continueOnCapturedContext: false))
            {
                case var events when events.Any() == false: return default;
                case var events: return FoldLeft(new T(), events);
            }
        }

        public async Task<T> TryRehydrateStateAt(Guid streamId, long version)
        {
            switch (await _eventReader.QueryEvents(streamId, fromVersion: 1).ConfigureAwait(continueOnCapturedContext: false))
            {
                case var events when events.Any() == false: return default;
                case var events when events.Count() < version: throw new InvalidOperationException($"State of the specified version({version}) does not exists.");
                case var events: return FoldLeft(new T(), events.TakeWhile((_, index) => index < version));
            }
        }

        private T FoldLeft(T seed, IEnumerable<object> events)
            => _eventHandler.HandleEvents(seed, events);

        private class DefaultSnapshotReader : ISnapshotReader<T>
        {
            public static readonly DefaultSnapshotReader Instance = new DefaultSnapshotReader();

            public Task<T> TryRestoreSnapshot(Guid streamId) => Task.FromResult<T>(default);
        }
    }
}
