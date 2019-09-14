namespace Loom.Messaging
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IProcessor
    {
        bool Accepts(Message input);

        Task<IEnumerable<object>> Process(Message input);

        Task PostProcess(Message input, IEnumerable<object> output);
    }
}
