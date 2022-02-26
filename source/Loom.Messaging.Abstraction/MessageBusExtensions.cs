using System;
using System.Threading;
using System.Threading.Tasks;

namespace Loom.Messaging
{
    public static class MessageBusExtensions
    {
        public static Task Send(
            this IMessageBus bus,
            Message message,
            string partitionKey,
            CancellationToken cancellationToken = default)
        {
            if (bus is null)
            {
                throw new ArgumentNullException(nameof(bus));
            }

            return bus.Send(new[] { message }, partitionKey, cancellationToken);
        }
    }
}
