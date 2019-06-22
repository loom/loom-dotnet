namespace Loom.EventSourcing.Azure
{
    using System;
    using Microsoft.Azure.Cosmos.Table;

    internal class QueueTicket : TableEntity
    {
        public QueueTicket()
        {
        }

        public QueueTicket(string entityType,
                           Guid streamId,
                           long startVersion,
                           long eventCount,
                           Guid transaction)
            : base(partitionKey: $"~{entityType}:{streamId}",
                   rowKey: $"{startVersion:D19}:{transaction}")
        {
            EntityType = entityType;
            StreamId = streamId;
            StartVersion = startVersion;
            EventCount = eventCount;
            Transaction = transaction;
        }

        public string EntityType { get; set; }

        public Guid StreamId { get; set; }

        public long StartVersion { get; set; }

        public long EventCount { get; set; }

        public Guid Transaction { get; set; }

        public static TableQuery<QueueTicket> CreateQuery(string entityType, Guid streamId)
        {
            string filter = $"PartitionKey eq '~{entityType}:{streamId}'";
            return new TableQuery<QueueTicket>().Where(filter);
        }
    }
}
