namespace Loom.EventSourcing.Azure
{
    using System;
    using System.Threading.Tasks;
    using AutoFixture;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using static StorageEmulator;

    [TestClass]
    public class BlobSnapshotReader_specs
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

        [TestMethod]
        public void sut_implements_ISnapshotReaderT()
        {
            typeof(BlobSnapshotReader<State>)
                .Should().Implement<ISnapshotReader<State>>();
        }

        [TestMethod]
        public async Task TryRestoreSnapshot_returns_null_if_snapshot_not_exists()
        {
            var sut = new BlobSnapshotReader<State>(SnapshotContainer);
            var streamId = Guid.NewGuid();

            State actual = await sut.TryRestoreSnapshot(streamId);

            actual.Should().BeNull();
        }

        [TestMethod]
        public async Task TryRestoreSnapshot_restores_snapshot_correctly_if_it_exists()
        {
            // Arrange
            var sut = new BlobSnapshotReader<State>(SnapshotContainer);
            var streamId = Guid.NewGuid();

            State state = new Fixture().Create<State>();

            IStateRehydrator<State> rehydrator =
                Mock.Of<IStateRehydrator<State>>();
            Mock.Get(rehydrator)
                .Setup(x => x.TryRehydrateState(streamId))
                .ReturnsAsync(state);

            var snapshotter = new BlobSnapshotter<State>(
                rehydrator, SnapshotContainer);
            await snapshotter.TakeSnapshot(streamId);

            // Act
            State actual = await sut.TryRestoreSnapshot(streamId);

            // Assert
            actual.Should().BeEquivalentTo(state);
        }
    }
}
