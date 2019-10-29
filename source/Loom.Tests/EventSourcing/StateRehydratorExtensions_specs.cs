namespace Loom.EventSourcing
{
    using System;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Loom.Testing;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class StateRehydratorExtensions_specs
    {
        [TestMethod, AutoData]
        public async Task RehydrateState_returns_state_correctly(
            IStateRehydrator<State1> rehydrator, Guid streamId, State1 state)
        {
            Mock.Get(rehydrator).Setup(x => x.TryRehydrateState(streamId)).ReturnsAsync(state);
            State1 actual = await rehydrator.RehydrateState(streamId);
            actual.Should().BeSameAs(state);
        }

        [TestMethod, AutoData]
        public async Task RehydrateState_fails_if_state_not_exists(
            IStateRehydrator<State1> rehydrator, Guid streamId)
        {
            Mock.Get(rehydrator).Setup(x => x.TryRehydrateState(streamId)).ReturnsAsync(default(State1));
            Func<Task> action = () => rehydrator.RehydrateState(streamId);
            await action.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}
