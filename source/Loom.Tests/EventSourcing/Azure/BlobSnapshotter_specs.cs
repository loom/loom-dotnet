using System;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FluentAssertions;
using Loom.Json;
using Loom.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;

namespace Loom.EventSourcing.Azure
{
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

        private static BlobSnapshotter<State> GenerateSut(IStateRehydrator<State> rehydrator) => new(
            rehydrator,
            jsonProcessor: new JsonProcessor(new JsonSerializer()),
            container: StorageEmulator.SnapshotContainer);

        private static BlobClient GetBlob(string streamId)
        {
            string blobName = $"{streamId}.json";
            return StorageEmulator.SnapshotContainer.GetBlobClient(blobName);
        }

        private static T GetContent<T>(BlobClient blob)
        {
            return blob.DownloadContent().Value.Content.ToObjectFromJson<T>();
        }

        [TestMethod]
        public void sut_implements_ISnapshotter()
        {
            typeof(BlobSnapshotter<State>).Should().Implement<ISnapshotter>();
        }

        [TestMethod, AutoData]
        public async Task TakeSnapshot_throws_exception_if_state_not_exists(
            string streamId, IStateRehydrator<State> rehydrator)
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
            string streamId, State state, IStateRehydrator<State> rehydrator)
        {
            // Arrange
            Mock.Get(rehydrator)
                .Setup(x => x.TryRehydrateState(streamId))
                .ReturnsAsync(state);

            BlobSnapshotter<State> sut = GenerateSut(rehydrator);

            // Act
            await sut.TakeSnapshot(streamId);

            // Assert
            State snapshot = GetContent<State>(blob: GetBlob(streamId));
            snapshot.Should().BeEquivalentTo(state);
        }

        [TestMethod, AutoData]
        public async Task TakeSnapshot_sets_blob_properties_correctly_if_snapshot_blob_not_exists(
            string streamId, State state, IStateRehydrator<State> rehydrator)
        {
            // Arrange
            Mock.Get(rehydrator)
                .Setup(x => x.TryRehydrateState(streamId))
                .ReturnsAsync(state);

            BlobSnapshotter<State> sut = GenerateSut(rehydrator);

            // Act
            await sut.TakeSnapshot(streamId);

            // Assert
            BlobClient blob = GetBlob(streamId);
            Response<BlobProperties> properties = blob.GetProperties();
            properties.Value.ContentType.Should().Be("application/json");
            properties.Value.ContentEncoding.Should().BeEquivalentTo("UTF-8");
        }

        [TestMethod, AutoData]
        public async Task TakeSnapshot_updates_snapshot_blob_if_exists(
            string streamId, State pastState, State newState, IStateRehydrator<State> rehydrator)
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
            State snapshot = GetContent<State>(blob: GetBlob(streamId));
            snapshot.Should().BeEquivalentTo(newState);
        }

        [TestMethod, AutoData]
        public async Task TakeSnapshot_sets_blob_properties_correctly_if_snapshot_blob_exists(
            string streamId, State state, IStateRehydrator<State> rehydrator)
        {
            // Arrange
            Mock.Get(rehydrator)
                .Setup(x => x.TryRehydrateState(streamId))
                .ReturnsAsync(state);

            BlobSnapshotter<State> sut = GenerateSut(rehydrator);

            await sut.TakeSnapshot(streamId);

            GetBlob(streamId).SetHttpHeaders(new BlobHttpHeaders
            {
                ContentType = "application/text",
                ContentEncoding = Encoding.ASCII.WebName,
            });

            // Act
            await sut.TakeSnapshot(streamId);

            // Assert
            BlobClient blob = GetBlob(streamId);
            Response<BlobProperties> properties = blob.GetProperties();
            properties.Value.ContentType.Should().Be("application/json");
            properties.Value.ContentEncoding.Should().BeEquivalentTo("UTF-8");
        }
    }
}
