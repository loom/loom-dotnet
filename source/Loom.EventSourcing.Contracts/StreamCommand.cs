using System;
using System.Reflection;

namespace Loom.EventSourcing
{
    public static class StreamCommand
    {
        public static StreamCommand<T> Create<T>(Guid streamId, T payload) => new(streamId, payload);

        public static bool TryDecompose(object value, out Guid streamId, out object payload)
        {
            if (value?.GetType() is Type type &&
                type.IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(StreamCommand<>))
            {
                (streamId, payload) = ((Guid, object))typeof(StreamCommand)
                    .GetMethod(nameof(Decompose), BindingFlags.Static | BindingFlags.NonPublic)
                    .MakeGenericMethod(type.GetGenericArguments())
                    .Invoke(obj: null, new[] { value });
                return true;
            }
            else
            {
                streamId = default;
                payload = default;
                return false;
            }
        }

        private static (Guid StreamId, object Payload) Decompose<T>(StreamCommand<T> command)
        {
            return (command.StreamId, command.Payload);
        }
    }
}
