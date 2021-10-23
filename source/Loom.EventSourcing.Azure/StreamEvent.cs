using System;
using System.Reflection;
using Loom.Json;
using Loom.Messaging;
using Microsoft.Azure.Cosmos.Table;

namespace Loom.EventSourcing.Azure
{
    internal class StreamEvent : TableEntity
    {
#pragma warning disable CS8618 // Properties will be set by Azure Table SDK.
        public StreamEvent()
#pragma warning restore CS8618
        {
        }

        public StreamEvent(string stateType,
                           string messageId,
                           string processId,
                           string? initiator,
                           string? predecessorId,
                           string streamId,
                           long version,
                           DateTime raisedTimeUtc,
                           string eventType,
                           string payload,
                           Guid transaction)
            : base(partitionKey: $"{stateType}:{streamId}", rowKey: FormatVersion(version))
        {
            StateType = stateType;
            StreamId = streamId;
            Version = version;
            RaisedTimeUtc = raisedTimeUtc;
            EventType = eventType;
            Payload = payload;
            MessageId = messageId;
            ProcessId = processId;
            Initiator = initiator;
            PredecessorId = predecessorId;
            Transaction = transaction;
        }

        public string StateType { get; set; }

        public string StreamId { get; set; }

        public long Version { get; set; }

        public DateTime RaisedTimeUtc { get; set; }

        public string EventType { get; set; }

        public string Payload { get; set; }

        public string MessageId { get; set; }

        public string ProcessId { get; set; }

        public string? Initiator { get; set; }

        public string? PredecessorId { get; set; }

        public Guid Transaction { get; set; }

        public static string FormatVersion(long version) => $"{version:D19}";

        private object DeserializePayload(IJsonProcessor jsonProcessor, Type type)
            => jsonProcessor.FromJson(json: Payload, dataType: type);

        public object RestorePayload(TypeResolver typeResolver, IJsonProcessor jsonProcessor)
            => DeserializePayload(jsonProcessor, ResolveType(typeResolver));

        public Message GenerateMessage(TypeResolver typeResolver, IJsonProcessor jsonProcessor)
            => GenerateMessage(ResolveType(typeResolver), jsonProcessor);

        private Type ResolveType(TypeResolver typeResolver)
            => typeResolver.TryResolveType(EventType)
            ?? throw new InvalidOperationException($"Could not resolve type with \"{EventType}\".");

        private Message GenerateMessage(Type type, IJsonProcessor jsonProcessor)
        {
            ConstructorInfo? constructor = typeof(StreamEvent<>)
                .MakeGenericType(type)
                .GetConstructor(types: new[]
                {
                    typeof(string),
                    typeof(long),
                    typeof(DateTime),
                    type,
                });

            object data = constructor!.Invoke(parameters: new object[]
            {
                StreamId,
                Version,
                RaisedTimeUtc,
                DeserializePayload(jsonProcessor, type),
            });

            return new Message(
                MessageId,
                ProcessId,
                Initiator,
                PredecessorId,
                data);
        }
    }
}
