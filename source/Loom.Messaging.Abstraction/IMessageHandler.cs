namespace Loom.Messaging
{
    using System.Threading.Tasks;

    public interface IMessageHandler
    {
        bool CanHandle(Message message);

        Task Handle(Message message);
    }
}
