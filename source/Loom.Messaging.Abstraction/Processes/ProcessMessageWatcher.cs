namespace Loom.Messaging.Processes
{
    using System.Threading;
    using System.Threading.Tasks;

    public class ProcessMessageWatcher : IMessageHandler
    {
        private readonly IProcessMessageCollector _collecotr;

        public ProcessMessageWatcher(IProcessMessageCollector collector)
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
