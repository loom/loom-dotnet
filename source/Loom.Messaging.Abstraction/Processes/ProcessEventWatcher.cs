namespace Loom.Messaging.Processes
{
    using System.Threading;
    using System.Threading.Tasks;

    public class ProcessEventWatcher : IMessageHandler
    {
        private readonly IProcessEventCollector _collecotr;

        public ProcessEventWatcher(IProcessEventCollector collector)
        {
            _collecotr = collector;
        }

        public bool CanHandle(Message message) => true;

        public Task Handle(Message message)
        {
            return _collecotr.Collect(message, CancellationToken.None);
        }
    }
}
