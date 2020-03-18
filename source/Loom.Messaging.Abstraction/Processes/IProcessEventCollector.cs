namespace Loom.Messaging.Processes
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IProcessEventCollector
    {
        Task Collect(Message message, CancellationToken cancellationToken);
    }
}
