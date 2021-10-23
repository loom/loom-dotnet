using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Loom.Json;
using Loom.Messaging;

namespace Loom.EventSourcing.EntityFrameworkCore
{
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

        public static Type ResolveType(this IEvent entity, TypeResolver typeResolver)
            => typeResolver.TryResolveType(entity.EventType)
            ?? throw new InvalidOperationException($"Could not resolve type with \"{entity.EventType}\".");

        public static Message GenerateMessage(
            this IEvent entity,
            TypeResolver typeResolver,
            IJsonProcessor jsonProcessor)
        {
            return entity.GenerateMessage(entity.ResolveType(typeResolver), jsonProcessor);
        }

        private static Message GenerateMessage(
            this IEvent entity,
            Type type,
            IJsonProcessor jsonProcessor)
        {
            ConstructorInfo constructor = typeof(StreamEvent<>)
                .MakeGenericType(type)
                .GetTypeInfo()
                .GetConstructor(new[]
                {
                    typeof(Guid),
                    typeof(long),
                    typeof(DateTime),
                    type,
                });

            object data = constructor.Invoke(parameters: new object[]
            {
                entity.StreamId,
                entity.Version,
                new DateTime(entity.RaisedTimeUtc.Ticks, DateTimeKind.Utc),
                jsonProcessor.FromJson(entity.Payload, type),
            });

            return new Message(
                entity.MessageId,
                entity.ProcessId,
                entity.Initiator,
                entity.PredecessorId,
                data);
        }

        public static void Deconstruct<TKey, TValue>(
            this KeyValuePair<TKey, TValue> pair,
            out TKey key,
            out TValue value)
        {
            (key, value) = (pair.Key, pair.Value);
        }
    }
}
