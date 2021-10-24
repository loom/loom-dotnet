using System.Threading;
using System.Threading.Tasks;

namespace Loom.Messaging
{
    public interface IMessageHandler
    {
        bool Accepts(Message message);

        // TODO: Remove default argument of parameter cancellationToken.
        Task Handle(Message message, CancellationToken cancellationToken = default);
    }
}
