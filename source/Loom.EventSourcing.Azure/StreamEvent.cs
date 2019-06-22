namespace Loom.EventSourcing.Azure
{
    using System;
    using System.Linq;
    using Loom.Messaging;
    using Microsoft.Azure.Cosmos.Table;

    internal class StreamEvent : TableEntity
    {
        public StreamEvent()
        {
        }

        public StreamEvent(string entityType,
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
            : base(partitionKey: $"{entityType}:{streamId}", rowKey: FormatVersion(version))
        {
            EntityType = entityType;
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

        public string EntityType { get; set; }

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
        public TracingProperties TracingProperties => new TracingProperties(OperationId, Contributor, ParentId);

        private static string FormatVersion(long version) => $"{version:D19}";

        public static TableQuery<StreamEvent> CreateQuery(
            string entityType, Guid streamId, long fromVersion)
        {
            string filter = CombineFilters(
                $"PartitionKey eq '{entityType}:{streamId}'",
                $"RowKey ge '{FormatVersion(fromVersion)}'");

            return new TableQuery<StreamEvent>().Where(filter);
        }

        public static TableQuery<StreamEvent> CreateQuery(QueueTicket queueTicket)
        {
            string filter = CombineFilters(
                $"PartitionKey eq '{queueTicket.EntityType}:{queueTicket.StreamId}'",
                $"RowKey ge '{queueTicket.StartVersion:D19}'",
                $"RowKey lt '{queueTicket.StartVersion + queueTicket.EventCount:D19}'",
                $"Transaction eq guid'{queueTicket.Transaction}'");

            return new TableQuery<StreamEvent>().Where(filter);
        }

        private static string CombineFilters(params string[] filters)
        {
            return filters.Aggregate(CombineFilters);
        }

        private static string CombineFilters(string filterA, string filterB)
        {
            return TableQuery.CombineFilters(filterA, "and", filterB);
        }
    }
}
