using System.Threading.Tasks;

namespace Loom.Messaging
{
    public interface IMessageHandler
    {
        bool Accepts(Message message);

        // TODO: Add a parameter of CancellationToken.
        Task Handle(Message message);
    }
}
