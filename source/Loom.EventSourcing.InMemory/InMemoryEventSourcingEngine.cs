namespace Loom.EventSourcing.InMemory
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Loom.Messaging;

    internal static class InMemoryEventSourcingEngine<T>
    {
        private static readonly ConcurrentDictionary<Guid, Dictionary<long, Message>> _store =
            new ConcurrentDictionary<Guid, Dictionary<long, Message>>();

        public static IEnumerable<Message> CollectEvents(
            Guid streamId,
            long startVersion,
            IEnumerable<object> events,
            TracingProperties tracingProperties)
        {
            Dictionary<long, Message> stream = _store.GetOrAdd(streamId, new Dictionary<long, Message>());

            long version = startVersion;
            var messages = new List<Message>();

            foreach (object payload in events)
            {
                object data = PackEvent(streamId, version, payload);
                var message = new Message($"{Guid.NewGuid()}", data, tracingProperties);

                stream.Add(version, message);

                messages.Add(message);

                version++;
            }

            return messages;
        }

        private static object PackEvent(Guid streamId, long version, object payload)
        {
            object[] arguments = new[] { streamId, version, DateTime.UtcNow, payload };

            return typeof(StreamEvent)
                .GetMethod("Create", BindingFlags.Static | BindingFlags.Public)
                .MakeGenericMethod(payload.GetType())
                .Invoke(obj: default, arguments);
        }

        public static IEnumerable<object> QueryEvents(Guid streamId, long fromVersion)
        {
            if (_store.TryGetValue(streamId, out Dictionary<long, Message> stream))
            {
                IEnumerable<object> query = from p in stream
                                            where p.Key >= fromVersion
                                            orderby p.Key
                                            let data = (dynamic)p.Value.Data
                                            let payload = data.Payload
                                            select payload;

                return query.ToList();
            }

            return Enumerable.Empty<object>();
        }

        public static IEnumerable<Message> QueryEventMessages(Guid streamId)
        {
            if (_store.TryGetValue(streamId, out Dictionary<long, Message> stream))
            {
                IEnumerable<Message> query = from p in stream
                                             orderby p.Key
                                             select p.Value;

                return query.ToList();
            }

            return Enumerable.Empty<Message>();
        }
    }
}
