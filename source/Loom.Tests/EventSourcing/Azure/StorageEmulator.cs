namespace Loom.EventSourcing.Azure
{
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Azure.Storage.Blob;

    public static class StorageEmulator
    {
        public static string EventStoreName => "UnitTestingEventStore";

        public static CloudTable EventStoreTable { get; } = CloudStorageAccount
            .DevelopmentStorageAccount
            .CreateCloudTableClient()
            .GetTableReference(EventStoreName);

        public static string SnapshotContainerName => "unit-testing-snapshot-store";

        public static CloudBlobContainer SnapshotContainer { get; } =
            Microsoft.Azure.Storage.CloudStorageAccount
                .DevelopmentStorageAccount
                .CreateCloudBlobClient()
                .GetContainerReference(SnapshotContainerName);

        public static async Task Initialize()
        {
            await EventStoreTable.DeleteIfExistsAsync();
            await EventStoreTable.CreateAsync();

            await SnapshotContainer.DeleteIfExistsAsync();
            await SnapshotContainer.CreateAsync();
        }
    }
}
