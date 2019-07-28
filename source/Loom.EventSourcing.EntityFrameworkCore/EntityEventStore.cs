namespace Loom.EventSourcing.EntityFrameworkCore
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;
    using Loom.Messaging;
    using Microsoft.EntityFrameworkCore;
    using Newtonsoft.Json;

    public class EntityEventStore<T> :
        IEventStore<T>, IEventCollector, IEventReader
    {
        private readonly Func<EventStoreContext> _contextFactory;
        private readonly TypeResolver _typeResolver;
        private readonly EventPublisher _publisher;

        public EntityEventStore(Func<EventStoreContext> contextFactory,
                                TypeResolver typeResolver,
                                IMessageBus eventBus)
        {
            _contextFactory = contextFactory;
            _typeResolver = typeResolver;
            _publisher = new EventPublisher(contextFactory, typeResolver, eventBus);
        }

        public Task CollectEvents(Guid streamId,
                                  long startVersion,
                                  IEnumerable<object> events,
                                  TracingProperties tracingProperties = default)
        {
            return SaveAndPublish(stateType: _typeResolver.ResolveTypeName<T>(),
                                  transaction: Guid.NewGuid(),
                                  streamId,
                                  startVersion,
                                  events.ToImmutableArray(),
                                  tracingProperties);
        }

        private async Task SaveAndPublish(string stateType,
                                          Guid transaction,
                                          Guid streamId,
                                          long startVersion,
                                          ImmutableArray<object> events,
                                          TracingProperties tracingProperties = default)
        {
            await SaveEvents().ConfigureAwait(continueOnCapturedContext: false);
            await PublishPendingEvents().ConfigureAwait(continueOnCapturedContext: false);

            async Task SaveEvents()
            {
                using (EventStoreContext context = _contextFactory.Invoke())
                {
                    for (int i = 0; i < events.Length; i++)
                    {
                        object source = events[i];

                        var streamEvent = new StreamEvent(
                            stateType,
                            streamId,
                            version: startVersion + i,
                            raisedTimeUtc: DateTime.UtcNow,
                            eventType: _typeResolver.ResolveTypeName(source.GetType()),
                            payload: JsonConvert.SerializeObject(source),
                            messageId: $"{Guid.NewGuid()}",
                            tracingProperties.OperationId,
                            tracingProperties.Contributor,
                            tracingProperties.ParentId,
                            transaction);

                        context.Add(streamEvent);
                        context.Add(new PendingEvent(streamEvent));
                    }

                    await context.SaveChangesAsync().ConfigureAwait(continueOnCapturedContext: false);
                }
            }

            Task PublishPendingEvents() => _publisher.PublishEvents(stateType, streamId);
        }

        public async Task<IEnumerable<object>> QueryEvents(
            Guid streamId, long fromVersion)
        {
            using (EventStoreContext context = _contextFactory.Invoke())
            {
                string stateType = _typeResolver.ResolveTypeName<T>();

                IQueryable<StreamEvent> query =
                    from e in context.StreamEvents
                    where
                        e.StateType == stateType &&
                        e.StreamId == streamId &&
                        e.Version >= fromVersion
                    orderby e.Version ascending
                    select e;

                IEnumerable<object> sequence =
                    from e in await query
                        .AsNoTracking()
                        .ToListAsync()
                        .ConfigureAwait(continueOnCapturedContext: false)
                    let value = e.Payload
                    let type = _typeResolver.TryResolveType(e.EventType)
                    select JsonConvert.DeserializeObject(value, type);

                return sequence.ToImmutableArray();
            }
        }
    }
}
