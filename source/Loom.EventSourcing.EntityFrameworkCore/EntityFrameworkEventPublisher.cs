namespace Loom.EventSourcing.EntityFrameworkCore
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;
    using Loom.Messaging;
    using Microsoft.EntityFrameworkCore;

    [Obsolete("This class has the concurrency vulnerability. Use EntityFrameworkPendingEventDetector and FlushEntityFrameworkEventsCommandExecutor instead.")]
    public class EntityFrameworkEventPublisher
    {
        private readonly Func<EventStoreContext> _contextFactory;
        private readonly TypeResolver _typeResolver;
        private readonly IMessageBus _eventBus;
        private readonly TimeSpan _minimumPendingTime;

        public EntityFrameworkEventPublisher(Func<EventStoreContext> contextFactory,
                                             TypeResolver typeResolver,
                                             IMessageBus eventBus,
                                             TimeSpan minimumPendingTime)
        {
            _contextFactory = contextFactory;
            _typeResolver = typeResolver;
            _eventBus = eventBus;
            _minimumPendingTime = minimumPendingTime;
        }

        public async Task PublishPendingEvents()
        {
            using (EventStoreContext context = _contextFactory.Invoke())
            {
                foreach ((string stateType, Guid streamId) in await GetStreams(context).ConfigureAwait(continueOnCapturedContext: false))
                {
                    await Flush(context, stateType, streamId).ConfigureAwait(continueOnCapturedContext: false);
                }
            }
        }

        private static async Task<IEnumerable<(string stateType, Guid streamId)>> GetStreams(EventStoreContext context)
        {
            var query = context.PendingEvents.Select(e => new { e.StateType, e.StreamId });
            var segments = await query.ToListAsync().ConfigureAwait(continueOnCapturedContext: false);
            return segments.Select(s => (s.StateType, s.StreamId)).ToImmutableArray();
        }

        private async Task Flush(EventStoreContext context, string stateType, Guid streamId)
        {
            IQueryable<PendingEvent> query = context.GetPendingEventsQuery(stateType, streamId);
            List<PendingEvent> pendingEvents = await query.ToListAsync().ConfigureAwait(continueOnCapturedContext: false);
            foreach (IEnumerable<PendingEvent> window in Window(pendingEvents))
            {
                string partitionKey = $"{streamId}";
                await _eventBus.Send(window.Select(GenerateMessage), partitionKey).ConfigureAwait(continueOnCapturedContext: false);
                context.PendingEvents.RemoveRange(window);
                await context.SaveChangesAsync().ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        private IEnumerable<IEnumerable<PendingEvent>> Window(IEnumerable<PendingEvent> stream)
        {
            Guid transaction = default;
            List<PendingEvent> fragment = null;

            foreach (PendingEvent entity in stream)
            {
                if (DateTime.UtcNow - entity.RaisedTimeUtc < _minimumPendingTime)
                {
                    break;
                }

                if (fragment == null)
                {
                    transaction = entity.Transaction;
                    fragment = new List<PendingEvent> { entity };
                    continue;
                }

                if (entity.Transaction != transaction)
                {
                    yield return fragment.ToImmutableArray();

                    transaction = entity.Transaction;
                    fragment = new List<PendingEvent> { entity };
                    continue;
                }

                fragment.Add(entity);
            }

            if (fragment?.Count > 0)
            {
                yield return fragment.ToImmutableArray();
            }
        }

        private Message GenerateMessage(PendingEvent entity) => entity.GenerateMessage(_typeResolver);
    }
}
