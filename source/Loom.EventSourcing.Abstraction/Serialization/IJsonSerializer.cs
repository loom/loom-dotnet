namespace Loom.EventSourcing.Serialization
{
    using System;

    public interface IJsonSerializer
    {
        string Serialize(object data);

        object Deserialize(string json, Type dataType);
    }
}
