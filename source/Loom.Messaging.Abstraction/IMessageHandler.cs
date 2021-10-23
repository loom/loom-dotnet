using System.Threading.Tasks;

namespace Loom.Messaging
{
    public interface IMessageHandler
    {
        bool Accepts(Message message);

        Task Handle(Message message);
    }
}
