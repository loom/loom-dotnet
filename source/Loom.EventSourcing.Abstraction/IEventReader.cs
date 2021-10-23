using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Loom.Messaging;

namespace Loom.EventSourcing
{
    public interface IEventReader
    {
        Task<IEnumerable<object>> QueryEvents(
            string streamId,
            long fromVersion);

        Task<IEnumerable<Message>> QueryEventMessages(
            string streamId,
            CancellationToken cancellationToken = default);
    }

    public interface IEventReader<T> : IEventReader
    {
    }
}
