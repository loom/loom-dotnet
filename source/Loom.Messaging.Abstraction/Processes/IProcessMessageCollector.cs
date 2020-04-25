namespace Loom.Messaging.Processes
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IProcessMessageCollector
    {
        Task Collect(Message message, CancellationToken cancellationToken);
    }
}
