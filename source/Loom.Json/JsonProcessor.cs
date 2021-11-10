using System;
using System.IO;
using Newtonsoft.Json;

namespace Loom.Json
{
    public sealed class JsonProcessor : IJsonProcessor
    {
        private readonly JsonSerializer _serializer;

        public JsonProcessor(JsonSerializer serializer)
        {
            _serializer = serializer;
        }

        public string ToJson(object data)
        {
            using var stringWriter = new StringWriter();
            using var jsonWriter = new JsonTextWriter(stringWriter);
            _serializer.Serialize(jsonWriter, data);
            return stringWriter.ToString();
        }

        public object FromJson(string json, Type dataType)
        {
            using var jsonReader = new JsonTextReader(new StringReader(json));
            return _serializer.Deserialize(jsonReader, dataType);
        }
    }
}
