namespace Loom.Messaging
{
    using System;
    using System.Threading.Tasks;

    public sealed class DelegatingMessageHandler : IMessageHandler
    {
        private readonly Func<Message, Task> _handle;

        public DelegatingMessageHandler(Func<Message, Task> handle)
        {
            _handle = handle;
        }

        public bool Accepts(Message message) => true;

        public Task Handle(Message message) => _handle.Invoke(message);
    }
}
