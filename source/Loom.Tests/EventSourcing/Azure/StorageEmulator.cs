namespace Loom.EventSourcing.Azure
{
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;

    public static class StorageEmulator
    {
        public static async Task Initialize()
        {
            await EventStoreTable.DeleteIfExistsAsync();
            await EventStoreTable.CreateAsync();
        }

        public static CloudStorageAccount StorageAccount
            => CloudStorageAccount.DevelopmentStorageAccount;

        public static string EventStoreName => "UnitTestingEventStore";

        public static CloudTable EventStoreTable { get; } = StorageAccount
            .CreateCloudTableClient()
            .GetTableReference(EventStoreName);
    }
}
