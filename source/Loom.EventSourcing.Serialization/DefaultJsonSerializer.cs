namespace Loom.EventSourcing.Serialization
{
    using System;
    using System.IO;
    using Newtonsoft.Json;

    public sealed class DefaultJsonSerializer : IJsonSerializer
    {
        private readonly JsonSerializer _serializer;
        private readonly Formatting _formatting;

        public DefaultJsonSerializer(JsonSerializer serializer,
                                     Formatting formatting)
        {
            _serializer = serializer;
            _formatting = formatting;
        }

        public DefaultJsonSerializer()
            : this(serializer: new JsonSerializer(), formatting: default)
        {
        }

        public string Serialize(object data)
        {
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonTextWriter(stringWriter)
            {
                Formatting = _formatting,
            })
            {
                _serializer.Serialize(jsonWriter, data);
                return stringWriter.ToString();
            }
        }

        public object Deserialize(string json, Type dataType)
        {
            using (var jsonReader = new JsonTextReader(new StringReader(json)))
            {
                return _serializer.Deserialize(jsonReader, dataType);
            }
        }
    }
}
