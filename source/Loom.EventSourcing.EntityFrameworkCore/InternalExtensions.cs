namespace Loom.EventSourcing.EntityFrameworkCore
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Loom.Messaging;
    using Newtonsoft.Json;

    internal static class InternalExtensions
    {
        public static IQueryable<PendingEvent> GetPendingEventsQuery(
            this EventStoreContext context,
            string stateType,
            Guid streamId)
        {
            return from e in context.PendingEvents
                   where e.StateType == stateType && e.StreamId == streamId
                   orderby e.Version
                   select e;
        }

        public static Message GenerateMessage(
            this PendingEvent entity, TypeResolver typeResolver)
        {
            Type type = typeResolver.TryResolveType(entity.EventType);

            ConstructorInfo constructor = typeof(StreamEvent<>)
                .MakeGenericType(type)
                .GetTypeInfo()
                .GetConstructor(new[] { typeof(Guid), typeof(long), typeof(DateTime), type });

            object data = constructor.Invoke(parameters: new object[]
            {
                entity.StreamId,
                entity.Version,
                new DateTime(entity.RaisedTimeUtc.Ticks, DateTimeKind.Utc),
                JsonConvert.DeserializeObject(entity.Payload, type),
            });

            return new Message(id: entity.MessageId, data, entity.TracingProperties);
        }
    }
}
