namespace Loom.EventSourcing.Azure
{
    using System;
    using Microsoft.Azure.Cosmos.Table;

    internal class QueueTicket : TableEntity
    {
        public QueueTicket()
        {
        }

        public QueueTicket(Guid streamId,
                           long startVersion,
                           long eventCount,
                           Guid transaction)
            : base(partitionKey: $"~{streamId}",
                   rowKey: $"{startVersion:D19}-{transaction}")
        {
            StreamId = streamId;
            StartVersion = startVersion;
            EventCount = eventCount;
            Transaction = transaction;
        }

        public Guid StreamId { get; set; }

        public long StartVersion { get; set; }

        public long EventCount { get; set; }

        public Guid Transaction { get; set; }

        public static TableQuery<QueueTicket> CreateQuery(Guid streamId)
        {
            string filter = $"PartitionKey eq '~{streamId}'";
            return new TableQuery<QueueTicket>().Where(filter);
        }
    }
}
