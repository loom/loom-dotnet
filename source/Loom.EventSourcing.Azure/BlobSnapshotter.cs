namespace Loom.EventSourcing.Azure
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Loom.Json;
    using Microsoft.Azure.Storage.Blob;

    public class BlobSnapshotter<T> : ISnapshotter
        where T : class
    {
        private static readonly Encoding _encoding = Encoding.UTF8;

        private readonly IStateRehydrator<T> _rehydrator;
        private readonly IJsonProcessor _jsonProcessor;
        private readonly CloudBlobContainer _container;

        public BlobSnapshotter(IStateRehydrator<T> rehydrator,
                               IJsonProcessor jsonProcessor,
                               CloudBlobContainer container)
        {
            _rehydrator = rehydrator;
            _jsonProcessor = jsonProcessor;
            _container = container;
        }

        public async Task TakeSnapshot(Guid streamId)
        {
            // TODO: Replace with IStateRehydrator<T>.RehydrateState() method.
            T state = await _rehydrator.TryRehydrateState(streamId).ConfigureAwait(continueOnCapturedContext: false);
            if (state == default)
            {
                string message = $"Could not rehydrate state with stream id '{streamId}'.";
                throw new InvalidOperationException(message);
            }

            CloudBlockBlob blob = GetBlobReference(streamId);
            await SetContent(blob, state).ConfigureAwait(continueOnCapturedContext: false);
            await SetProperties(blob).ConfigureAwait(continueOnCapturedContext: false);
        }

        private CloudBlockBlob GetBlobReference(Guid streamId)
        {
            return _container.GetBlockBlobReference($"{streamId}.json");
        }

        private async Task SetContent(CloudBlockBlob blob, T state)
        {
            string content = _jsonProcessor.ToJson(state);
            using (var source = new MemoryStream(_encoding.GetBytes(content)))
            {
                await blob.UploadFromStreamAsync(source).ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        private static Task SetProperties(CloudBlockBlob blob)
        {
            blob.Properties.ContentType = "application/json";
            blob.Properties.ContentEncoding = _encoding.WebName;
            return blob.SetPropertiesAsync();
        }
    }
}
