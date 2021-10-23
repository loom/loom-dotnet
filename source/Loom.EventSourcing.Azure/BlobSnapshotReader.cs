using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Loom.Json;

namespace Loom.EventSourcing.Azure
{
    public class BlobSnapshotReader<T> : ISnapshotReader<T>
        where T : class
    {
        private readonly BlobContainerClient _container;
        private readonly IJsonProcessor _jsonProcessor;

        public BlobSnapshotReader(BlobContainerClient container,
                                  IJsonProcessor jsonProcessor)
        {
            _container = container;
            _jsonProcessor = jsonProcessor;
        }

        public async Task<T?> TryRestoreSnapshot(string streamId)
        {
            BlobClient blob = GetBlob(streamId);
            return await blob.ExistsAsync().ConfigureAwait(continueOnCapturedContext: false)
                 ? await RestoreSnapshot(blob).ConfigureAwait(continueOnCapturedContext: false)
                 : default;
        }

        private BlobClient GetBlob(string streamId)
        {
            return _container.GetBlobClient(blobName: $"{streamId}.json");
        }

        private async Task<T> RestoreSnapshot(BlobClient blob)
        {
            byte[] bytes = await GetBytes(blob).ConfigureAwait(continueOnCapturedContext: false);
            string content = Encoding.UTF8.GetString(bytes);
            return (T)_jsonProcessor.FromJson(content, dataType: typeof(T));
        }

        private static async Task<byte[]> GetBytes(BlobClient blob)
        {
            Response<BlobDownloadResult> response = await blob
                .DownloadContentAsync()
                .ConfigureAwait(continueOnCapturedContext: false);

            return response.Value.Content.ToArray();
        }
    }
}
