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
                   where Compare(t.PartitionKey, "~") >= 0
                   select t;
        }

        public static IQueryable<QueueTicket> BuildQueueTicketsQuery(
            this CloudTable table, string stateType, string streamId)
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
                   where Compare(e.RowKey, $"{queueTicket.StartVersion:D19}") >= 0
                   where Compare(e.RowKey, $"{queueTicket.StartVersion + queueTicket.EventCount:D19}") < 0
                   where e.Transaction == queueTicket.Transaction
                   select e;
        }

        public static IQueryable<StreamEvent> BuildStreamEventQuery(
            this CloudTable table, string stateType, string streamId, long fromVersion)
        {
            return from e in table.CreateQuery<StreamEvent>()
                   where e.PartitionKey == $"{stateType}:{streamId}"
                   where Compare(e.RowKey, StreamEvent.FormatVersion(fromVersion)) >= 0
                   select e;
        }

        private static int Compare(string a, string b) => string.Compare(a, b, StringComparison.Ordinal);
    }
}
