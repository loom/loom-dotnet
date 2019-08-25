namespace Loom.Json
{
    using System;

    public interface IJsonProcessor
    {
        string ToJson(object data);

        object FromJson(string json, Type dataType);
    }
}
