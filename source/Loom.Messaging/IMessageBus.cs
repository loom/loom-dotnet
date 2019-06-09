namespace Loom.Messaging
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IMessageBus
    {
        Task Send(IEnumerable<Message> messages);
    }
}
