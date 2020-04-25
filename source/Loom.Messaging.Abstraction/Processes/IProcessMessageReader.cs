namespace Loom.Messaging.Processes
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IProcessMessageReader
    {
        Task<IEnumerable<Message>> Query(string operationId, CancellationToken cancellationToken);
    }
}
