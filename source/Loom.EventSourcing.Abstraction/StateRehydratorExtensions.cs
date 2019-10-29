namespace Loom.EventSourcing
{
    using System;
    using System.Threading.Tasks;

    public static class StateRehydratorExtensions
    {
        public static async Task<T> RehydrateState<T>(
            this IStateRehydrator<T> rehydrator, Guid streamId)
            where T : class
        {
            if (rehydrator is null)
            {
                throw new ArgumentNullException(nameof(rehydrator));
            }

            return await rehydrator.TryRehydrateState(streamId).ConfigureAwait(continueOnCapturedContext: false) switch
            {
                T state => state,
                _ => throw new InvalidOperationException($"Could not rehydrate state with stream id '{streamId}'.")
            };
        }
    }
}
