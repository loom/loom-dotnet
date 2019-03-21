﻿namespace Loom.EventSourcing
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IEventCollector
    {
        // TODO: Change type of parameter 'contributor' to string?.
        // TODO: Change type of parameter 'parentId' to string?.
        Task Collect(
            Guid streamId,
            long firstVersion,
            IEnumerable<object> events,
            string operationId,
            string contributor,
            string parentId);
    }
}