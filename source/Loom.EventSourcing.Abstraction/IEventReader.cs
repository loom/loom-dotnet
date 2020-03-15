namespace Loom.EventSourcing
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Loom.Messaging;

    public interface IEventReader
    {
        Task<IEnumerable<object>> QueryEvents(Guid streamId, long fromVersion);

        Task<IEnumerable<Message>> QueryEventMessages(
            Guid streamId,
            CancellationToken cancellationToken = default);
    }

    public interface IEventReader<T> : IEventReader
    {
    }
}
