namespace Loom.EventSourcing.Azure
{
    using System;
    using Microsoft.Azure.Cosmos.Table;

    internal class QueueTicket : TableEntity
    {
        public QueueTicket()
        {
        }

        public QueueTicket(string stateType,
                           Guid streamId,
                           long startVersion,
                           long eventCount,
                           Guid transaction)
            : base(partitionKey: $"~{stateType}:{streamId}",
                   rowKey: $"{startVersion:D19}:{transaction}")
        {
            StateType = stateType;
            StreamId = streamId;
            StartVersion = startVersion;
            EventCount = eventCount;
            Transaction = transaction;
        }

        public string StateType { get; set; }

        public Guid StreamId { get; set; }

        public long StartVersion { get; set; }

        public long EventCount { get; set; }

        public Guid Transaction { get; set; }

        public static TableQuery<QueueTicket> CreateQuery(string stateType, Guid streamId)
        {
            string filter = $"PartitionKey eq '~{stateType}:{streamId}'";
            return new TableQuery<QueueTicket>().Where(filter);
        }
    }
}
