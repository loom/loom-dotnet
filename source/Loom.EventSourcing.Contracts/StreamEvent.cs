using System;
using System.Reflection;

namespace Loom.EventSourcing
{
    public static class StreamEvent
    {
        public static StreamEvent<T> Create<T>(Guid streamId,
                                               long version,
                                               DateTime raisedTimeUtc,
                                               T payload)
        {
            return new StreamEvent<T>(streamId, version, raisedTimeUtc, payload);
        }

        public static bool TryDecompose(object value,
                                        out Guid streamId,
                                        out long version,
                                        out DateTime raisedTimeUtc,
                                        out object payload)
        {
            if (value?.GetType() is Type type &&
                type.IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(StreamEvent<>))
            {
                (streamId, version, raisedTimeUtc, payload) = (StreamEvent<object>)typeof(StreamEvent)
                    .GetMethod(nameof(ToWeaklyTyped), BindingFlags.Static | BindingFlags.NonPublic)
                    .MakeGenericMethod(type.GetGenericArguments())
                    .Invoke(obj: null, new[] { value });
                return true;
            }
            else
            {
                streamId = default;
                version = default;
                raisedTimeUtc = default;
                payload = default;
                return false;
            }
        }

        private static StreamEvent<object> ToWeaklyTyped<T>(StreamEvent<T> value)
            => new(value.StreamId, value.Version, value.RaisedTimeUtc, value.Payload);
    }
}
