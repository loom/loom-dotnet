namespace Loom.EventSourcing.Azure
{
    using System;
    using Microsoft.Azure.Cosmos.Table;
    using static Microsoft.Azure.Cosmos.Table.TableQuery;
    using static Microsoft.Azure.Cosmos.Table.QueryComparisons;
    using static Microsoft.Azure.Cosmos.Table.TableOperators;

    internal class StreamEvent : TableEntity
    {
        public string EventType { get; set; }

        public string EventData { get; set; }

        public StreamEvent()
        {
        }

        public StreamEvent(
            Guid streamId, long version, string eventType, string eventData)
            : base(partitionKey: $"{streamId}", rowKey: FormatVersion(version))
        {
            EventType = eventType;
            EventData = eventData;
        }

        private static string FormatVersion(long version) => $"{version:D19}";

        public static TableQuery<StreamEvent> CreateQuery(
            Guid streamId, long fromVersion)
        {
            string filter = CombineFilters(
                GenerateFilterCondition(
                    nameof(PartitionKey),
                    Equal,
                    $"{streamId}"),
                And,
                GenerateFilterCondition(
                    nameof(RowKey),
                    GreaterThanOrEqual,
                    FormatVersion(fromVersion)));

            return new TableQuery<StreamEvent>().Where(filter);
        }
    }
}
