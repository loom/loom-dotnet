namespace Loom.EventSourcing.EntityFrameworkCore
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;
    using Loom.Json;
    using Loom.Messaging;
    using Microsoft.EntityFrameworkCore;

    public class EntityEventStore<TContext, TState> :
        IEventStore<TState>, IEventCollector, IEventReader
        where TContext : EventStoreContext
    {
        private readonly Func<TContext> _contextFactory;
        private readonly TypeResolver _typeResolver;
        private readonly IJsonProcessor _jsonProcessor;
        private readonly EventPublisher _publisher;

        public EntityEventStore(Func<TContext> contextFactory,
                                TypeResolver typeResolver,
                                IJsonProcessor jsonProcessor,
                                IMessageBus eventBus)
        {
            _contextFactory = contextFactory;
            _typeResolver = typeResolver;
            _jsonProcessor = jsonProcessor;
            _publisher = new EventPublisher(contextFactory, typeResolver, jsonProcessor, eventBus);
        }

        public Task CollectEvents(Guid streamId,
                                  long startVersion,
                                  IEnumerable<object> events,
                                  TracingProperties tracingProperties = default)
        {
            return SaveAndPublish(stateType: _typeResolver.ResolveTypeName<TState>(),
                streamId: streamId,
                startVersion: startVersion,
                events: events.ToImmutableArray(),
                tracingProperties: tracingProperties,
                transaction: Guid.NewGuid());
        }

        private async Task SaveAndPublish(string stateType,
                                          Guid streamId,
                                          long startVersion,
                                          ImmutableArray<object> events,
                                          TracingProperties tracingProperties,
                                          Guid transaction)
        {
            await SaveEvents().ConfigureAwait(continueOnCapturedContext: false);
            await PublishPendingEvents().ConfigureAwait(continueOnCapturedContext: false);

            async Task SaveEvents()
            {
                using TContext context = _contextFactory.Invoke();
                AddEntities(context, stateType, streamId, startVersion, events, tracingProperties, transaction);
                await context.SaveChangesAsync().ConfigureAwait(continueOnCapturedContext: false);
            }

            Task PublishPendingEvents() => _publisher.PublishEvents(stateType, streamId);
        }

        protected virtual void AddEntities(TContext context,
                                           string stateType,
                                           Guid streamId,
                                           long startVersion,
                                           ImmutableArray<object> events,
                                           TracingProperties tracingProperties,
                                           Guid transaction)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            for (int i = 0; i < events.Length; i++)
            {
                object source = events[i];

                var streamEvent = new StreamEvent(
                    stateType,
                    streamId,
                    version: startVersion + i,
                    raisedTimeUtc: DateTime.UtcNow,
                    eventType: _typeResolver.ResolveTypeName(source.GetType()),
                    payload: _jsonProcessor.ToJson(source),
                    messageId: $"{Guid.NewGuid()}",
                    tracingProperties.OperationId,
                    tracingProperties.Contributor,
                    tracingProperties.ParentId,
                    transaction);

                context.Add(streamEvent);
                context.Add(new PendingEvent(streamEvent));
            }
        }

        public async Task<IEnumerable<object>> QueryEvents(
            Guid streamId, long fromVersion)
        {
            using TContext context = _contextFactory.Invoke();

            string stateType = _typeResolver.ResolveTypeName<TState>();

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
                select _jsonProcessor.FromJson(value, type);

            return sequence.ToImmutableArray();
        }
    }
}
