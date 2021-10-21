namespace Loom.EventSourcing.EntityFrameworkCore
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Loom.Messaging;

    public sealed class EntityPendingEventScanner : IPendingEventScanner
    {
        private readonly Func<EventStoreContext> _contextFactory;
        private readonly IMessageBus _commandBus;
        private readonly TimeSpan _minimumPendingTime;

        public EntityPendingEventScanner(Func<EventStoreContext> contextFactory,
                                         IMessageBus commandBus,
                                         TimeSpan minimumPendingTime)
        {
            _contextFactory = contextFactory;
            _commandBus = commandBus;
            _minimumPendingTime = minimumPendingTime;
        }

        public EntityPendingEventScanner(Func<EventStoreContext> contextFactory, IMessageBus commandBus)
            : this(contextFactory, commandBus, minimumPendingTime: TimeSpan.Zero)
        {
        }

        public async Task ScanPendingEvents()
        {
            using (EventStoreContext context = _contextFactory.Invoke())
            {
                await ScanPendingEvents(context).ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        private Task ScanPendingEvents(EventStoreContext context)
        {
            DateTime filter = DateTime.UtcNow - _minimumPendingTime;

            var query = from e in context.PendingEvents
                        where e.RaisedTimeUtc <= filter
                        group e by new { e.StateType, e.StreamId } into s
                        select s.Key;

            return query.ForEach(t => SendFlushCommand(t.StateType, t.StreamId));
        }

        private Task SendFlushCommand(string stateType, Guid streamId)
        {
            Message message = Envelop(command: new FlushEvents(stateType, streamId));
            return Send(message, partitionKey: $"{streamId}");
        }

        private static Message Envelop(FlushEvents command)
        {
            string commandId = $"{Guid.NewGuid()}";
            string operationId = $"{Guid.NewGuid()}";
            string contributor = typeof(EntityPendingEventScanner).FullName;
            var tracingProperties = new TracingProperties(operationId, contributor, parentId: default);
            return Message.Create(commandId, command, tracingProperties);
        }

        private Task Send(Message message, string partitionKey)
        {
            return _commandBus.Send(new[] { message }, partitionKey);
        }
    }
}
