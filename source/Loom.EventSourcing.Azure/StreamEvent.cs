namespace Loom.EventSourcing.Azure
{
    using System;
    using System.Reflection;
    using Loom.Json;
    using Loom.Messaging;
    using Microsoft.Azure.Cosmos.Table;

    internal class StreamEvent : TableEntity
    {
        public StreamEvent()
        {
        }

        public StreamEvent(string stateType,
                           Guid streamId,
                           long version,
                           DateTime raisedTimeUtc,
                           string eventType,
                           string payload,
                           string messageId,
                           string operationId,
                           string contributor,
                           string parentId,
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
            OperationId = operationId;
            Contributor = contributor;
            ParentId = parentId;
            Transaction = transaction;
        }

        public string StateType { get; set; }

        public Guid StreamId { get; set; }

        public long Version { get; set; }

        public DateTime RaisedTimeUtc { get; set; }

        public string EventType { get; set; }

        public string Payload { get; set; }

        public string MessageId { get; set; }

        public string OperationId { get; set; }

        public string Contributor { get; set; }

        public string ParentId { get; set; }

        public Guid Transaction { get; set; }

        [IgnoreProperty]
        public TracingProperties TracingProperties
            => new TracingProperties(OperationId, Contributor, ParentId);

        public static string FormatVersion(long version) => $"{version:D19}";

        private object DeserializePayload(IJsonProcessor jsonProcessor, Type type)
            => jsonProcessor.FromJson(json: Payload, dataType: type);

        public object RestorePayload(
            TypeResolver typeResolver, IJsonProcessor jsonProcessor)
        {
            Type type = typeResolver.TryResolveType(EventType);
            return DeserializePayload(jsonProcessor, type);
        }

        public Message GenerateMessage(
            TypeResolver typeResolver, IJsonProcessor jsonProcessor)
        {
            Type type = typeResolver.TryResolveType(EventType);

            ConstructorInfo constructor = typeof(StreamEvent<>)
                .MakeGenericType(type)
                .GetTypeInfo()
                .GetConstructor(types: new[]
                {
                    typeof(Guid),
                    typeof(long),
                    typeof(DateTime),
                    type,
                });

            object data = constructor.Invoke(parameters: new object[]
            {
                StreamId,
                Version,
                RaisedTimeUtc,
                DeserializePayload(jsonProcessor, type),
            });

            return new Message(id: MessageId, data, TracingProperties);
        }
    }
}
