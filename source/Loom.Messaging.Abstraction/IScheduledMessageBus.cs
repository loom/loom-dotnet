using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Loom.Messaging
{
    public interface IScheduledMessageBus
    {
        Task Send(
            IEnumerable<Message> messages,
            string partitionKey,
            DateTime scheduledTimeUtc,
            CancellationToken cancellationToken = default);
    }
}
