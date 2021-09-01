namespace Loom.EventSourcing.Azure
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Loom.Json;
    using Microsoft.Azure.Storage.Blob;

    public class BlobSnapshotReader<T> : ISnapshotReader<T>
        where T : class
    {
        private readonly CloudBlobContainer _container;
        private readonly IJsonProcessor _jsonProcessor;

        public BlobSnapshotReader(CloudBlobContainer container,
                                  IJsonProcessor jsonProcessor)
        {
            _container = container;
            _jsonProcessor = jsonProcessor;
        }

        public async Task<T?> TryRestoreSnapshot(Guid streamId)
        {
            CloudBlockBlob blob = GetBlobReference(streamId);
            return await blob.ExistsAsync().ConfigureAwait(continueOnCapturedContext: false)
                 ? await RestoreSnapshot(blob).ConfigureAwait(continueOnCapturedContext: false)
                 : default;
        }

        private CloudBlockBlob GetBlobReference(Guid streamId)
        {
            return _container.GetBlockBlobReference($"{streamId}.json");
        }

        private async Task<T> RestoreSnapshot(CloudBlockBlob blob)
        {
            using (var stream = new MemoryStream())
            {
                await blob.DownloadToStreamAsync(stream).ConfigureAwait(continueOnCapturedContext: false);
                string content = Encoding.UTF8.GetString(stream.ToArray());
                return (T)_jsonProcessor.FromJson(content, dataType: typeof(T));
            }
        }
    }
}
