using System;

namespace Loom.Json
{
    public interface IJsonProcessor
    {
        string ToJson(object data);

        object FromJson(string json, Type dataType);
    }
}
