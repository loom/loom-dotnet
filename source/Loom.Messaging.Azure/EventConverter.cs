namespace Loom.Messaging.Azure
{
    using System;
    using System.Text;
    using Loom.Json;
    using Microsoft.Azure.EventHubs;

    public sealed class EventConverter : IEventConverter
    {
        private readonly IJsonProcessor _jsonProcessor;
        private readonly TypeResolver _typeResolver;

        public EventConverter(
            IJsonProcessor jsonProcessor, TypeResolver typeResolver)
        {
            _jsonProcessor = jsonProcessor;
            _typeResolver = typeResolver;
        }

        public EventData ConvertToEvent(Message message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            object data = message.Data;
            TracingProperties tracingProperties = message.TracingProperties;
            byte[] array = Encoding.UTF8.GetBytes(_jsonProcessor.ToJson(data));
            return new EventData(array)
            {
                Properties =
                {
                    ["Id"] = message.Id,
                    ["Type"] = _typeResolver.ResolveTypeName(data.GetType()),
                    ["OperationId"] = tracingProperties.OperationId,
                    ["Contributor"] = tracingProperties.Contributor,
                    ["ParentId"] = tracingProperties.ParentId,
                },
            };
        }

        public Message? TryConvertToMessage(EventData eventData)
        {
            if (eventData is null)
            {
                throw new ArgumentNullException(nameof(eventData));
            }

            return TryResolveDataType(eventData) switch
            {
                Type dataType => new Message(GetId(eventData),
                                             GetData(eventData, dataType),
                                             GetTracingProperties(eventData)),
                _ => null,
            };
        }

        private Type? TryResolveDataType(EventData eventData)
        {
            eventData.Properties.TryGetValue("Type", out object value);
            return value is string typeName ? _typeResolver.TryResolveType(typeName) : null;
        }

        private static string GetId(EventData eventData)
        {
            eventData.Properties.TryGetValue("Id", out object value);
            return value is string id ? id : $"{Guid.NewGuid()}";
        }

        private object GetData(EventData eventData, Type dataType)
        {
            string json = Encoding.UTF8.GetString(eventData.Body.Array);
            return _jsonProcessor.FromJson(json, dataType);
        }

        private static TracingProperties GetTracingProperties(EventData eventData)
        {
            return new TracingProperties(GetOperationId(eventData),
                                         GetContributor(eventData),
                                         GetParentId(eventData));
        }

        private static string GetOperationId(EventData eventData)
        {
            eventData.Properties.TryGetValue("OperationId", out object value);
            return value is string operationId ? operationId : $"{Guid.NewGuid()}";
        }

        private static string? GetContributor(EventData eventData)
        {
            eventData.Properties.TryGetValue("Contributor", out object value);
            return value is string contributor ? contributor : null;
        }

        private static string? GetParentId(EventData eventData)
        {
            eventData.Properties.TryGetValue("ParentId", out object value);
            return value is string parentId ? parentId : null;
        }
    }
}
