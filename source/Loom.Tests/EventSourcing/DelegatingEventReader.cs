using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Loom.Messaging;

namespace Loom.EventSourcing
{
    internal class DelegatingEventReader : IEventReader
    {
        private readonly Func<Guid, long, Task<IEnumerable<object>>> _function;

        public DelegatingEventReader(
            Func<Guid, long, Task<IEnumerable<object>>> function)
        {
            _function = function;
        }

        public Task<IEnumerable<object>> QueryEvents(
            Guid streamId, long fromVersion)
        {
            return _function.Invoke(streamId, fromVersion);
        }

        public Task<IEnumerable<Message>> QueryEventMessages(
            Guid streamId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
