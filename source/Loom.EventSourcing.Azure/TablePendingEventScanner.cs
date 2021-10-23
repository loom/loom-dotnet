using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Loom.Messaging;
using Microsoft.Azure.Cosmos.Table;

namespace Loom.EventSourcing.Azure
{
    public sealed class TablePendingEventScanner : IPendingEventScanner
    {
        private readonly CloudTable _table;
        private readonly IMessageBus _commandBus;
        private readonly TimeSpan _minimumPendingTime;

        public TablePendingEventScanner(CloudTable table,
                                        IMessageBus commandBus,
                                        TimeSpan minimumPendingTime)
        {
            _table = table;
            _commandBus = commandBus;
            _minimumPendingTime = minimumPendingTime;
        }

        public TablePendingEventScanner(CloudTable table, IMessageBus commandBus)
            : this(table, commandBus, minimumPendingTime: TimeSpan.Zero)
        {
        }

        public Task ScanPendingEvents()
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
            var marks = new HashSet<(string StateType, string StreamId)>();
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

        private Task SendFlushCommand(string stateType, string streamId)
        {
            Message message = Envelop(command: new FlushEvents(stateType, streamId));
            return Send(message, partitionKey: $"{streamId}");
        }

        private static Message Envelop(FlushEvents command)
        {
            string commandId = $"{Guid.NewGuid()}";
            string processId = $"{Guid.NewGuid()}";
            string? initiator = typeof(TablePendingEventScanner).FullName;
            return new(commandId, processId, initiator, predecessorId: default, command);
        }

        private Task Send(Message message, string partitionKey)
        {
            return _commandBus.Send(new[] { message }, partitionKey);
        }
    }
}
