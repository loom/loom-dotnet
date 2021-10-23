using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos.Table;

namespace Loom.EventSourcing.Azure
{
    public static class StorageEmulator
    {
        public static string EventStoreName => "UnitTestingEventStore";

        public static CloudTable EventStoreTable { get; } = CloudStorageAccount
            .DevelopmentStorageAccount
            .CreateCloudTableClient()
            .GetTableReference(EventStoreName);

        public static string SnapshotContainerName => "unit-testing-snapshot-store";

        public static BlobContainerClient SnapshotContainer { get; } =
            new("UseDevelopmentStorage=true", SnapshotContainerName);

        public static async Task Initialize()
        {
            await EventStoreTable.DeleteIfExistsAsync();
            await EventStoreTable.CreateAsync();

            await SnapshotContainer.DeleteIfExistsAsync();
            await SnapshotContainer.CreateAsync();
        }
    }
}
