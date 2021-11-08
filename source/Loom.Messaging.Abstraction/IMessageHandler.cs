using System.Threading;
using System.Threading.Tasks;

namespace Loom.Messaging
{
    public interface IMessageHandler
    {
        bool CanHandle(Message message);

        Task Handle(Message message, CancellationToken cancellationToken);
    }
}
