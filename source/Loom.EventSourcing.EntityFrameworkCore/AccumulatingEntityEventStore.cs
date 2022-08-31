using System;
using Loom.Json;
using Loom.Messaging;

namespace Loom.EventSourcing.EntityFrameworkCore
{
    public class AccumulatingEntityEventStore<T> : EntityEventStore<T>
        where T : class, IState
    {
        public AccumulatingEntityEventStore(
            Func<EventStoreContext> contextFactory,
            IUniquePropertyDetector uniquePropertyDetector,
            TypeResolver typeResolver,
            IJsonProcessor jsonProcessor,
            IMessageBus eventBus,
            IEventHandler handler)
            : base(contextFactory,
                   uniquePropertyDetector,
                   typeResolver,
                   jsonProcessor,
                   eventBus)
        {
        }

        protected override void Foo()
        {
            base.Foo();
        }
    }
}
