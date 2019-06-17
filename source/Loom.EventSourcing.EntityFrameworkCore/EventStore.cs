namespace Loom.EventSourcing.EntityFrameworkCore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Loom.Messaging;
    using Microsoft.EntityFrameworkCore;
    using Newtonsoft.Json;

    public class EventStore : IEventCollector, IEventReader
    {
        private readonly Func<EventStoreContext> _contextFactory;
        private readonly TypeResolver _typeResolver;

        public EventStore(Func<EventStoreContext> contextFactory, TypeResolver typeResolver)
        {
            _contextFactory = contextFactory;
            _typeResolver = typeResolver;
        }

        public async Task CollectEvents(Guid streamId,
                                        long startVersion,
                                        IEnumerable<object> events,
                                        TracingProperties tracingProperties = default)
        {
            using (EventStoreContext context = _contextFactory.Invoke())
            {
                context.StreamEvents.AddRange(events.Select(SerializeEvent));
                await context.SaveChangesAsync().ConfigureAwait(false);
            }

            StreamEvent SerializeEvent(object source, int index) =>
                new StreamEvent(
                    streamId,
                    version: startVersion + index,
                    raisedTimeUtc: DateTime.UtcNow,
                    eventType: _typeResolver.ResolveTypeName(source.GetType()),
                    payload: JsonConvert.SerializeObject(source));
        }

        public async Task<IEnumerable<object>> QueryEvents(
            Guid streamId, long fromVersion)
        {
            using (EventStoreContext context = _contextFactory.Invoke())
            {
                IQueryable<StreamEvent> query =
                    from e in context.StreamEvents
                    where
                        e.StreamId == streamId &&
                        e.Version >= fromVersion
                    orderby e.Version ascending
                    select e;

                return from e in await query.ToListAsync().ConfigureAwait(false)
                       let value = e.Payload
                       let type = _typeResolver.TryResolveType(e.EventType)
                       select JsonConvert.DeserializeObject(value, type);
            }
        }
    }
}
