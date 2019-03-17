namespace Loom.EventSourcing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class StateRehydrator<T> : IStateRehydrator<T>
        where T : IVersioned, new()
    {
        private readonly ISnapshotReader<T> _snapshotReader;
        private readonly IEventReader _eventReader;
        private readonly IEventHandler<T> _eventHandler;

        public StateRehydrator(
            ISnapshotReader<T> snapshotReader,
            IEventReader eventReader,
            IEventHandler<T> eventHandler)
        {
            _snapshotReader = snapshotReader;
            _eventReader = eventReader;
            _eventHandler = eventHandler;
        }

        public async Task<T> TryRehydrateState(Guid streamId)
        {
            switch (await _snapshotReader.TryRestoreSnapshot(streamId))
            {
                case T snapshot: return await Rehydrate(streamId, snapshot);
                default: return await TryRehydrate(streamId);
            }
        }

        private async Task<T> Rehydrate(Guid streamId, T snapshot)
        {
            switch (await _eventReader.QueryEventPayloads(streamId, afterVersion: snapshot.Version))
            {
                case var payloads when payloads.Any() == false: return snapshot;
                case var payloads: return FoldLeft(snapshot, payloads);
            }
        }

        private async Task<T> TryRehydrate(Guid streamId)
        {
            switch (await _eventReader.QueryEventPayloads(streamId, afterVersion: default))
            {
                case var payloads when payloads.Any() == false: return default;
                case var payloads: return FoldLeft(new T(), payloads);
            }
        }

        private T FoldLeft(T seed, IEnumerable<object> payloads)
            => payloads.Aggregate(seed, _eventHandler.Handle);
    }
}
