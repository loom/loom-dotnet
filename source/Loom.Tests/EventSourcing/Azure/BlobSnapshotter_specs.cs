namespace Loom.EventSourcing.Azure
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using AutoFixture;
    using FluentAssertions;
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
            var reader = new StreamReader(stream, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }

        [TestMethod]
        public void sut_implements_ISnapshotter()
        {
            typeof(BlobSnapshotter<State>).Should().Implement<ISnapshotter>();
        }

        [TestMethod]
        public async Task TakeSnapshot_throws_exception_if_state_not_exists()
        {
            var streamId = Guid.NewGuid();
            var sut = new BlobSnapshotter<State>(
                rehydrator: Mock.Of<IStateRehydrator<State>>(),
                container: StorageEmulator.SnapshotContainer);

            Func<Task> action = () => sut.TakeSnapshot(streamId);

            await action.Should().ThrowAsync<InvalidOperationException>();
        }

        [TestMethod]
        public async Task TaksSnapshot_creates_new_snapshot_blob_if_not_exists()
        {
            // Arrange
            var streamId = Guid.NewGuid();
            State state = new Fixture().Create<State>();

            IStateRehydrator<State> rehydrator =
                Mock.Of<IStateRehydrator<State>>();
            Mock.Get(rehydrator)
                .Setup(x => x.TryRehydrateState(streamId))
                .ReturnsAsync(state);

            CloudBlobContainer container = StorageEmulator.SnapshotContainer;

            var sut = new BlobSnapshotter<State>(rehydrator, container);

            // Act
            await sut.TakeSnapshot(streamId);

            // Assert
            ICloudBlob blob = await
                container.GetBlobReferenceFromServerAsync($"{streamId}.json");

            string content = await DownloadContent(blob);
            State snapshot = JsonConvert.DeserializeObject<State>(content);

            snapshot.Should().BeEquivalentTo(state);
        }

        [TestMethod]
        public async Task TakeSnapshot_sets_blob_properties_correctly_if_snapshot_blob_not_exists()
        {
            // Arrange
            var streamId = Guid.NewGuid();
            State state = new Fixture().Create<State>();

            IStateRehydrator<State> rehydrator =
                Mock.Of<IStateRehydrator<State>>();
            Mock.Get(rehydrator)
                .Setup(x => x.TryRehydrateState(streamId))
                .ReturnsAsync(state);

            CloudBlobContainer container = StorageEmulator.SnapshotContainer;

            var sut = new BlobSnapshotter<State>(rehydrator, container);

            // Act
            await sut.TakeSnapshot(streamId);

            // Assert
            ICloudBlob blob = await
                container.GetBlobReferenceFromServerAsync($"{streamId}.json");
            blob.Properties.ContentType.Should().Be("application/json");
            blob.Properties.ContentEncoding.Should().BeEquivalentTo("UTF-8");
        }

        [TestMethod]
        public async Task TakeSnapshot_updates_snapshot_blob_if_exists()
        {
            // Arrange
            IStateRehydrator<State> rehydrator =
                Mock.Of<IStateRehydrator<State>>();
            CloudBlobContainer container = StorageEmulator.SnapshotContainer;
            var sut = new BlobSnapshotter<State>(rehydrator, container);

            var streamId = Guid.NewGuid();
            var builder = new Fixture();

            Mock.Get(rehydrator)
                .Setup(x => x.TryRehydrateState(streamId))
                .ReturnsAsync(builder.Create<State>());

            await sut.TakeSnapshot(streamId);

            State state = builder.Create<State>();

            Mock.Get(rehydrator)
                .Setup(x => x.TryRehydrateState(streamId))
                .ReturnsAsync(state);

            // Act
            await sut.TakeSnapshot(streamId);

            // Assert
            ICloudBlob blob = await
                container.GetBlobReferenceFromServerAsync($"{streamId}.json");

            string content = await DownloadContent(blob);
            State snapshot = JsonConvert.DeserializeObject<State>(content);

            snapshot.Should().BeEquivalentTo(state);
        }

        [TestMethod]
        public async Task TakeSnapshot_sets_blob_properties_correctly_if_snapshot_blob_exists()
        {
            // Arrange
            IStateRehydrator<State> rehydrator =
                Mock.Of<IStateRehydrator<State>>();
            CloudBlobContainer container = StorageEmulator.SnapshotContainer;
            var sut = new BlobSnapshotter<State>(rehydrator, container);

            var streamId = Guid.NewGuid();

            Mock.Get(rehydrator)
                .Setup(x => x.TryRehydrateState(streamId))
                .ReturnsAsync(new Fixture().Create<State>());

            await sut.TakeSnapshot(streamId);

            ICloudBlob blob = await
                container.GetBlobReferenceFromServerAsync($"{streamId}.json");
            blob.Properties.ContentType = "application/text";
            blob.Properties.ContentEncoding = Encoding.ASCII.WebName;
            await blob.SetPropertiesAsync();

            // Act
            await sut.TakeSnapshot(streamId);

            // Assert
            ICloudBlob actual = await
                container.GetBlobReferenceFromServerAsync($"{streamId}.json");
            actual.Properties.ContentType.Should().Be("application/json");
            actual.Properties.ContentEncoding.Should().BeEquivalentTo("UTF-8");
        }
    }
}
