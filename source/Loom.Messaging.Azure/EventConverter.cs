using System;
using System.Text;
using Loom.Json;
using Microsoft.Azure.EventHubs;

namespace Loom.Messaging.Azure
{
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
            byte[] array = Encoding.UTF8.GetBytes(_jsonProcessor.ToJson(data));
            return new EventData(array)
            {
                Properties =
                {
                    ["Id"] = message.Id,
                    ["Type"] = _typeResolver.TryResolveTypeName(data.GetType()),
                    ["ProcessId"] = message.ProcessId,
                    ["Initiator"] = message.Initiator,
                    ["PredecessorId"] = message.PredecessorId,
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
                Type dataType => GetData(eventData, dataType) switch
                {
                    object data => new Message(GetId(eventData),
                                               GetProcessId(eventData),
                                               GetInitiator(eventData),
                                               GetPredecessorId(eventData),
                                               data),
                    _ => null,
                },
                _ => null,
            };
        }

        private Type? TryResolveDataType(EventData eventData)
        {
            eventData.Properties.TryGetValue("Type", out object? value);
            return value is string typeName ? _typeResolver.TryResolveType(typeName) : null;
        }

        private static string GetId(EventData eventData)
        {
            eventData.Properties.TryGetValue("Id", out object? value);
            return value is string id ? id : $"{Guid.NewGuid()}";
        }

        private object? GetData(EventData eventData, Type dataType)
        {
            if (eventData.Body.Array == null)
            {
                return null;
            }

            string json = Encoding.UTF8.GetString(eventData.Body.Array);
            return _jsonProcessor.FromJson(json, dataType);
        }

        private static string GetProcessId(EventData eventData)
        {
            eventData.Properties.TryGetValue("ProcessId", out object? value);
            return value is string processId ? processId : $"{Guid.NewGuid()}";
        }

        private static string? GetInitiator(EventData eventData)
        {
            eventData.Properties.TryGetValue("Initiator", out object? value);
            return value is string initiator ? initiator : null;
        }

        private static string? GetPredecessorId(EventData eventData)
        {
            eventData.Properties.TryGetValue("PredecessorId", out object? value);
            return value is string predecessorId ? predecessorId : null;
        }
    }
}
