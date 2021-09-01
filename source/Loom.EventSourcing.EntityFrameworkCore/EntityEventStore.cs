namespace Loom.EventSourcing.EntityFrameworkCore
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Loom.Json;
    using Loom.Messaging;
    using Microsoft.EntityFrameworkCore;

    public class EntityEventStore<T> :
        IEventStore<T>, IEventCollector, IEventReader
    {
        private readonly Func<EventStoreContext> _contextFactory;
        private readonly IUniquePropertyDetector _uniquePropertyDetector;
        private readonly TypeResolver _typeResolver;
        private readonly IJsonProcessor _jsonProcessor;
        private readonly EventPublisher _publisher;

        public EntityEventStore(Func<EventStoreContext> contextFactory,
                                IUniquePropertyDetector uniquePropertyDetector,
                                TypeResolver typeResolver,
                                IJsonProcessor jsonProcessor,
                                IMessageBus eventBus)
        {
            _contextFactory = contextFactory;
            _uniquePropertyDetector = uniquePropertyDetector;
            _typeResolver = typeResolver;
            _jsonProcessor = jsonProcessor;
            _publisher = new EventPublisher(contextFactory, typeResolver, jsonProcessor, eventBus);
        }

        public EntityEventStore(Func<EventStoreContext> contextFactory,
                                TypeResolver typeResolver,
                                IJsonProcessor jsonProcessor,
                                IMessageBus eventBus)
            : this(contextFactory, uniquePropertyDetector: default, typeResolver, jsonProcessor, eventBus)
        {
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
                using EventStoreContext context = _contextFactory.Invoke();

                for (int i = 0; i < events.Length; i++)
                {
                    object source = events[i];

                    var streamEvent = new StreamEvent(
                        stateType,
                        streamId,
                        version: startVersion + i,
                        raisedTimeUtc: DateTime.UtcNow,
                        eventType: ResolveName(source),
                        payload: _jsonProcessor.ToJson(source),
                        messageId: $"{Guid.NewGuid()}",
                        tracingProperties.OperationId,
                        tracingProperties.Contributor,
                        tracingProperties.ParentId,
                        transaction);

                    context.Add(streamEvent);
                    context.Add(PendingEvent.Create(streamEvent));
                }

                List<UniqueProperty> uniqueProperties = await context
                    .Set<UniqueProperty>()
                    .Where(p => p.StateType == stateType && p.StreamId == streamId)
                    .AsNoTracking()
                    .ToListAsync()
                    .ConfigureAwait(continueOnCapturedContext: false);

                foreach ((string name, string value) in GetUniqueProperties(events))
                {
                    UniqueProperty property = uniqueProperties.Find(p => p.Name == name);

                    if (value == null)
                    {
                        if (property != null)
                        {
                            context.UniqueProperties.Remove(property);
                        }
                    }
                    else
                    {
                        if (property == null)
                        {
                            context.Add(new UniqueProperty(stateType, streamId, name, value));
                        }
                        else
                        {
                            property.SetValue(value);
                            context.Update(property);
                        }
                    }
                }

                await context.SaveChangesAsync().ConfigureAwait(continueOnCapturedContext: false);
            }

            Task PublishPendingEvents() => _publisher.PublishEvents(stateType, streamId);
        }

        private string ResolveName(object source)
            => _typeResolver.ResolveTypeName(source.GetType())
            ?? throw new InvalidOperationException($"Could not resolve the name of type {source.GetType()}.");

        private IReadOnlyDictionary<string, string> GetUniqueProperties(IEnumerable<object> events)
        {
            var uniqueProperties = new Dictionary<string, string>();
            foreach (object source in events)
            {
                foreach ((string name, string value) in GetUniqueProperties(source))
                {
                    uniqueProperties[name] = value;
                }
            }

            return uniqueProperties;
        }

        private IReadOnlyDictionary<string, string> GetUniqueProperties(object source)
        {
            return _uniquePropertyDetector switch
            {
                IUniquePropertyDetector service => service.GetUniqueProperties(source),
                _ => ImmutableDictionary<string, string>.Empty,
            };
        }

        public async Task<IEnumerable<object>> QueryEvents(
            Guid streamId, long fromVersion)
        {
            CancellationToken cancellationToken = CancellationToken.None;
            List<StreamEvent> source = await GetEntities(streamId, fromVersion, cancellationToken)
                                            .ConfigureAwait(continueOnCapturedContext: false);
            return source.Select(RestorePayload).ToImmutableArray();
        }

        public async Task<IEnumerable<Message>> QueryEventMessages(
            Guid streamId,
            CancellationToken cancellationToken = default)
        {
            int fromVersion = 1;
            List<StreamEvent> source = await GetEntities(streamId, fromVersion, cancellationToken)
                                            .ConfigureAwait(continueOnCapturedContext: false);
            return source.Select(GenerateMessage).ToImmutableArray();
        }

        private async Task<List<StreamEvent>> GetEntities(
            Guid streamId,
            long fromVersion,
            CancellationToken cancellationToken)
        {
            string stateType = _typeResolver.ResolveTypeName<T>();
            using EventStoreContext context = _contextFactory.Invoke();
            IQueryable<StreamEvent> query = from e in context.StreamEvents
                                            where
                                                e.StateType == stateType &&
                                                e.StreamId == streamId &&
                                                e.Version >= fromVersion
                                            orderby e.Version ascending
                                            select e;
            return await query.AsNoTracking()
                              .ToListAsync(cancellationToken)
                              .ConfigureAwait(continueOnCapturedContext: false);
        }

        private object RestorePayload(StreamEvent entity)
            => _jsonProcessor.FromJson(entity.Payload, entity.ResolveType(_typeResolver));

        private Message GenerateMessage(StreamEvent entity)
            => entity.GenerateMessage(_typeResolver, _jsonProcessor);
    }
}
