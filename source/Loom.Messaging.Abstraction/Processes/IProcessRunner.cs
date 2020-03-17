namespace Loom.Messaging.Processes
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IProcessRunner
    {
        Task<ProcessResult> Run(Message trigger,
                                ProcessOptions options,
                                CancellationToken cancellationToken);
    }
}
