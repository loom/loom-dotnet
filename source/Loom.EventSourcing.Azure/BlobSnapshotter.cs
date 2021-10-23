using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Loom.Json;

namespace Loom.EventSourcing.Azure
{
    public class BlobSnapshotter<T> : ISnapshotter
        where T : class
    {
        private static readonly Encoding _encoding = Encoding.UTF8;

        private readonly IStateRehydrator<T> _rehydrator;
        private readonly IJsonProcessor _jsonProcessor;
        private readonly BlobContainerClient _container;

        public BlobSnapshotter(IStateRehydrator<T> rehydrator,
                               IJsonProcessor jsonProcessor,
                               BlobContainerClient container)
        {
            _rehydrator = rehydrator;
            _jsonProcessor = jsonProcessor;
            _container = container;
        }

        public async Task TakeSnapshot(string streamId)
        {
            T state = await _rehydrator.RehydrateState(streamId).ConfigureAwait(continueOnCapturedContext: false);
            BlobClient blob = GetBlob(streamId);
            await SetContent(blob, state).ConfigureAwait(continueOnCapturedContext: false);
            await SetProperties(blob).ConfigureAwait(continueOnCapturedContext: false);
        }

        private BlobClient GetBlob(string streamId)
        {
            return _container.GetBlobClient(blobName: $"{streamId}.json");
        }

        private async Task SetContent(BlobClient blob, T state)
        {
            string content = _jsonProcessor.ToJson(state);
            using var source = new MemoryStream(_encoding.GetBytes(content));
            await blob.UploadAsync(source, overwrite: true).ConfigureAwait(continueOnCapturedContext: false);
        }

        private static Task SetProperties(BlobClient blob)
        {
            return blob.SetHttpHeadersAsync(new BlobHttpHeaders
            {
                ContentType = "application/json",
                ContentEncoding = _encoding.WebName,
            });
        }
    }
}
