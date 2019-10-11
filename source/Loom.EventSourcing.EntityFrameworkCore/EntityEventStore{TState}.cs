namespace Loom.EventSourcing.EntityFrameworkCore
{
    using System;
    using Loom.Json;
    using Loom.Messaging;

    public class EntityEventStore<TState> :
        EntityEventStore<EventStoreContext, TState>
    {
        public EntityEventStore(Func<EventStoreContext> contextFactory,
                                TypeResolver typeResolver,
                                IJsonProcessor jsonProcessor,
                                IMessageBus eventBus)
            : base(contextFactory, typeResolver, jsonProcessor, eventBus)
        {
        }
    }
}
