using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Loom.EventSourcing
{
    public static class EventReaderExtensions
    {
        public static Task<IEnumerable<object>> QueryEvents(
            this IEventReader eventReader,
            string streamId,
            CancellationToken cancellationToken = default)
        {
            if (eventReader is null)
            {
                throw new ArgumentNullException(nameof(eventReader));
            }

            return eventReader.QueryEvents(streamId, fromVersion: 1, cancellationToken);
        }
    }
}
