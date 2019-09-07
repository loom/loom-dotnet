namespace Loom.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IScheduledMessageBus
    {
        Task Send(IEnumerable<Message> messages,
                  string partitionKey,
                  DateTime scheduledTimeUtc);
    }
}
