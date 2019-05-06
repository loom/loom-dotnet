namespace Loom.EventSourcing.Azure
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.Storage.Blob;
    using Newtonsoft.Json;

    public class BlobSnapshotter<T> : ISnapshotter
    {
        private static readonly Encoding _encoding = Encoding.UTF8;

        private readonly IStateRehydrator<T> _rehydrator;
        private readonly CloudBlobContainer _container;

        public BlobSnapshotter(
            IStateRehydrator<T> rehydrator, CloudBlobContainer container)
        {
            _rehydrator = rehydrator;
            _container = container;
        }

        public async Task TakeSnapshot(Guid streamId)
        {
            // TODO: Replace with IStateRehydrator<T>.RehydrateState() method.
            T state = await _rehydrator.TryRehydrateState(streamId);
            if (state == default)
            {
                string message = $"Could not rehydrate state with stream id '{streamId}'.";
                throw new InvalidOperationException(message);
            }

            CloudBlockBlob blob = GetBlobReference(streamId);
            await SetContent(blob, state);
            await SetProperties(blob);
        }

        private CloudBlockBlob GetBlobReference(Guid streamId)
        {
            return _container.GetBlockBlobReference($"{streamId}.json");
        }

        private static Task SetContent(CloudBlockBlob blob, T state)
        {
            string content = JsonConvert.SerializeObject(state);
            var source = new MemoryStream(_encoding.GetBytes(content));
            return blob.UploadFromStreamAsync(source);
        }

        private static Task SetProperties(CloudBlockBlob blob)
        {
            blob.Properties.ContentType = "application/json";
            blob.Properties.ContentEncoding = _encoding.WebName;
            return blob.SetPropertiesAsync();
        }
    }
}
