namespace Loom.EventSourcing.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Loom.Messaging;
    using Microsoft.Azure.Cosmos.Table;

    public sealed class TablePendingEventDetector
    {
        private readonly CloudTable _table;
        private readonly IMessageBus _commandBus;
        private readonly TimeSpan _minimumPendingTime;

        public TablePendingEventDetector(CloudTable table,
                                         IMessageBus commandBus,
                                         TimeSpan minimumPendingTime)
        {
            _table = table;
            _commandBus = commandBus;
            _minimumPendingTime = minimumPendingTime;
        }

        public TablePendingEventDetector(CloudTable table, IMessageBus commandBus)
            : this(table, commandBus, minimumPendingTime: TimeSpan.Zero)
        {
        }

        public Task ScanQueueTickets()
        {
            IQueryable<QueueTicket> query = BuildQueueTicketsQuery();
            return ScanQueueTickets(query);
        }

        private IQueryable<QueueTicket> BuildQueueTicketsQuery()
        {
            return _table.BuildQueueTicketsQuery();
        }

        private Task ScanQueueTickets(IQueryable<QueueTicket> query)
        {
            var marks = new HashSet<(string stateType, Guid streamId)>();
            return query.ForEach(async t =>
            {
                if (DateTimeOffset.UtcNow - t.Timestamp >= _minimumPendingTime &&
                    marks.Contains((t.StateType, t.StreamId)) == false)
                {
                    marks.Add((t.StateType, t.StreamId));
                    await SendFlushCommand(t.StateType, t.StreamId).ConfigureAwait(continueOnCapturedContext: false);
                }
            });
        }

        private Task SendFlushCommand(string stateType, Guid streamId)
        {
            var command = new FlushTableEvents(stateType, streamId);

            var message = new Message(
                id: $"{Guid.NewGuid()}",
                data: command,
                new TracingProperties(
                    operationId: $"{Guid.NewGuid()}",
                    contributor: typeof(TablePendingEventDetector).FullName,
                    parentId: default));

            return _commandBus.Send(new[] { message }, $"{streamId}");
        }
    }
}
