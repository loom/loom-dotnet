namespace Loom.Json
{
    using System;

    public static class JsonProcessorExtensions
    {
        public static T FromJson<T>(this IJsonProcessor jsonProcessor, string json)
        {
            if (jsonProcessor is null)
            {
                throw new ArgumentNullException(nameof(jsonProcessor));
            }

            return (T)jsonProcessor.FromJson(json, dataType: typeof(T));
        }
    }
}
