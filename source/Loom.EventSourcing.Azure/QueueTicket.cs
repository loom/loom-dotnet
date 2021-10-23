using System;
using Microsoft.Azure.Cosmos.Table;

namespace Loom.EventSourcing.Azure
{
    internal class QueueTicket : TableEntity
    {
#pragma warning disable CS8618 // Properties will be set by Azure Table SDK.
        public QueueTicket()
#pragma warning restore CS8618
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
    }
}
