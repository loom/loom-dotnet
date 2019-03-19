namespace Loom.EventSourcing
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    internal class DelegatingEventReader : IEventReader
    {
        private readonly Func<Guid, long, Task<IEnumerable<object>>> _function;

        public DelegatingEventReader(
            Func<Guid, long, Task<IEnumerable<object>>> function)
        {
            _function = function;
        }

        public Task<IEnumerable<object>> QueryEventPayloads(
            Guid streamId, long afterVersion)
        {
            return _function.Invoke(streamId, afterVersion);
        }
    }
}
