namespace Loom.EventSourcing
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public static class EventReaderExtensions
    {
        public static Task<IEnumerable<object>> QueryEvents(
            this IEventReader eventReader, Guid streamId)
        {
            if (eventReader is null)
            {
                throw new ArgumentNullException(nameof(eventReader));
            }

            return eventReader.QueryEvents(streamId, fromVersion: 1);
        }
    }
}
