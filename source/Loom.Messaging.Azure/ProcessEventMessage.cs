namespace Loom.Messaging.Azure
{
    using Newtonsoft.Json;

    internal class ProcessEventMessage
    {
        public ProcessEventMessage(
            string id,
            string dataType,
            string dataJson,
            string operationId,
            string contributor,
            string parentId)
        {
            Id = id;
            DataType = dataType;
            DataJson = dataJson;
            OperationId = operationId;
            Contributor = contributor;
            ParentId = parentId;
        }

        public string Id { get; }

        public string DataType { get; }

        public string DataJson { get; }

        public string OperationId { get; }

        public string Contributor { get; }

        public string ParentId { get; }

        [JsonProperty("ttl")]
        public int TimeToLiveInSeconds => 60 * 10;
    }
}
