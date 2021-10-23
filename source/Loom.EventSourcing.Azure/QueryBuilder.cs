using System;
using System.Linq;
using Microsoft.Azure.Cosmos.Table;

namespace Loom.EventSourcing.Azure
{
    internal static class QueryBuilder
    {
        public static IQueryable<QueueTicket> BuildQueueTicketsQuery(this CloudTable table)
        {
            return from t in table.CreateQuery<QueueTicket>()
                   where t.PartitionKey.CompareTo("~") >= 0
                   select t;
        }

        public static IQueryable<QueueTicket> BuildQueueTicketsQuery(
            this CloudTable table, string stateType, Guid streamId)
        {
            return from t in table.CreateQuery<QueueTicket>()
                   where t.PartitionKey == $"~{stateType}:{streamId}"
                   select t;
        }

        public static IQueryable<StreamEvent> BuildStreamEventQuery(
            this CloudTable table, QueueTicket queueTicket)
        {
            return from e in table.CreateQuery<StreamEvent>()
                   where e.PartitionKey == $"{queueTicket.StateType}:{queueTicket.StreamId}"
                   where e.RowKey.CompareTo($"{queueTicket.StartVersion:D19}") >= 0
                   where e.RowKey.CompareTo($"{queueTicket.StartVersion + queueTicket.EventCount:D19}") < 0
                   where e.Transaction == queueTicket.Transaction
                   select e;
        }

        public static IQueryable<StreamEvent> BuildStreamEventQuery(
            this CloudTable table, string stateType, Guid streamId, long fromVersion)
        {
            return from e in table.CreateQuery<StreamEvent>()
                   where e.PartitionKey == $"{stateType}:{streamId}"
                   where e.RowKey.CompareTo(StreamEvent.FormatVersion(fromVersion)) >= 0
                   select e;
        }
    }
}
