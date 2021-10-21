using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Loom.Messaging;

namespace Loom.EventSourcing.InMemory
{
    public class InMemoryEventSourcingEngine<T>
    {
        private readonly ConcurrentDictionary<Guid, Dictionary<long, Message>> _store = new();

        public static InMemoryEventSourcingEngine<T> Default { get; } = new InMemoryEventSourcingEngine<T>();

        internal IEnumerable<Message> CollectEvents(
            Guid streamId,
            long startVersion,
            IEnumerable<object> events,
            TracingProperties tracingProperties)
        {
            if (events is null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            Dictionary<long, Message> stream = _store.GetOrAdd(streamId, new Dictionary<long, Message>());

            long version = startVersion;
            var messages = new List<Message>();

            foreach (object payload in events)
            {
                object data = PackEvent(streamId, version, payload);
                var message = Message.Create($"{Guid.NewGuid()}", data, tracingProperties);

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

        internal IEnumerable<object> QueryEvents(Guid streamId, long fromVersion)
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

        internal IEnumerable<Message> QueryEventMessages(Guid streamId)
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

        public IReadOnlyDictionary<Guid, IEnumerable<Message>> Snapshot()
        {
            return _store.ToDictionary(t => t.Key, t => Serialize(t.Value));
        }

        private static IEnumerable<Message> Serialize(Dictionary<long, Message> eventMessages)
        {
            return eventMessages.OrderBy(t => t.Key).Select(t => t.Value).ToList();
        }
    }
}
