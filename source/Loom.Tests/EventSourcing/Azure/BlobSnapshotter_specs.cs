namespace Loom.EventSourcing.Azure
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Loom.Json;
    using Loom.Testing;
    using Microsoft.Azure.Storage.Blob;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Newtonsoft.Json;

    [TestClass]
    public class BlobSnapshotter_specs
    {
        public class State
        {
            public State(int value1, string value2, Guid value3)
            {
                Value1 = value1;
                Value2 = value2;
                Value3 = value3;
            }

            public int Value1 { get; }

            public string Value2 { get; }

            public Guid Value3 { get; }
        }

        private static async Task<string> DownloadContent(ICloudBlob blob)
        {
            var stream = new MemoryStream();
            await blob.DownloadToStreamAsync(stream);
            stream.Seek(offset: 0, SeekOrigin.Begin);
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                return await reader.ReadToEndAsync();
            }
        }

        private static BlobSnapshotter<State> GenerateSut(IStateRehydrator<State> rehydrator) =>
            new BlobSnapshotter<State>(
                rehydrator,
                jsonProcessor: new JsonProcessor(new JsonSerializer()),
                container: StorageEmulator.SnapshotContainer);

        private static Task<ICloudBlob> GetBlob(Guid streamId)
        {
            CloudBlobContainer container = StorageEmulator.SnapshotContainer;
            string blobName = $"{streamId}.json";
            return container.GetBlobReferenceFromServerAsync(blobName);
        }

        [TestMethod]
        public void sut_implements_ISnapshotter()
        {
            typeof(BlobSnapshotter<State>).Should().Implement<ISnapshotter>();
        }

        [TestMethod, AutoData]
        public async Task TakeSnapshot_throws_exception_if_state_not_exists(
            Guid streamId, IStateRehydrator<State> rehydrator)
        {
            // Arrange
            Mock.Get(rehydrator)
                .Setup(x => x.TryRehydrateState(streamId))
                .ReturnsAsync(default(State));

            BlobSnapshotter<State> sut = GenerateSut(rehydrator);

            // Act
            Func<Task> action = () => sut.TakeSnapshot(streamId);

            // Assert
            await action.Should().ThrowAsync<InvalidOperationException>();
        }

        [TestMethod, AutoData]
        public async Task TaksSnapshot_creates_new_snapshot_blob_if_not_exists(
            Guid streamId, State state, IStateRehydrator<State> rehydrator)
        {
            // Arrange
            Mock.Get(rehydrator)
                .Setup(x => x.TryRehydrateState(streamId))
                .ReturnsAsync(state);

            BlobSnapshotter<State> sut = GenerateSut(rehydrator);

            // Act
            await sut.TakeSnapshot(streamId);

            // Assert
            ICloudBlob blob = await GetBlob(streamId);

            string content = await DownloadContent(blob);
            State snapshot = JsonConvert.DeserializeObject<State>(content);

            snapshot.Should().BeEquivalentTo(state);
        }

        [TestMethod, AutoData]
        public async Task TakeSnapshot_sets_blob_properties_correctly_if_snapshot_blob_not_exists(
            Guid streamId, State state, IStateRehydrator<State> rehydrator)
        {
            // Arrange
            Mock.Get(rehydrator)
                .Setup(x => x.TryRehydrateState(streamId))
                .ReturnsAsync(state);

            BlobSnapshotter<State> sut = GenerateSut(rehydrator);

            // Act
            await sut.TakeSnapshot(streamId);

            // Assert
            ICloudBlob blob = await GetBlob(streamId);
            blob.Properties.ContentType.Should().Be("application/json");
            blob.Properties.ContentEncoding.Should().BeEquivalentTo("UTF-8");
        }

        [TestMethod, AutoData]
        public async Task TakeSnapshot_updates_snapshot_blob_if_exists(
            Guid streamId, State pastState, State newState, IStateRehydrator<State> rehydrator)
        {
            // Arrange
            Mock.Get(rehydrator)
                .Setup(x => x.TryRehydrateState(streamId))
                .ReturnsAsync(pastState);

            BlobSnapshotter<State> sut = GenerateSut(rehydrator);

            await sut.TakeSnapshot(streamId);

            Mock.Get(rehydrator)
                .Setup(x => x.TryRehydrateState(streamId))
                .ReturnsAsync(newState);

            // Act
            await sut.TakeSnapshot(streamId);

            // Assert
            ICloudBlob blob = await GetBlob(streamId);

            string content = await DownloadContent(blob);
            State snapshot = JsonConvert.DeserializeObject<State>(content);

            snapshot.Should().BeEquivalentTo(newState);
        }

        [TestMethod, AutoData]
        public async Task TakeSnapshot_sets_blob_properties_correctly_if_snapshot_blob_exists(
            Guid streamId, State state, IStateRehydrator<State> rehydrator)
        {
            // Arrange
            Mock.Get(rehydrator)
                .Setup(x => x.TryRehydrateState(streamId))
                .ReturnsAsync(state);

            BlobSnapshotter<State> sut = GenerateSut(rehydrator);

            await sut.TakeSnapshot(streamId);

            ICloudBlob blob = await GetBlob(streamId);
            blob.Properties.ContentType = "application/text";
            blob.Properties.ContentEncoding = Encoding.ASCII.WebName;
            await blob.SetPropertiesAsync();

            // Act
            await sut.TakeSnapshot(streamId);

            // Assert
            ICloudBlob actual = await GetBlob(streamId);
            actual.Properties.ContentType.Should().Be("application/json");
            actual.Properties.ContentEncoding.Should().BeEquivalentTo("UTF-8");
        }
    }
}
