namespace Loom.EventSourcing
{
    using System;
    using System.Threading.Tasks;

    public interface IRehydrator<T>
    {
        // TODO: Change return type to Task<T?>.
        Task<T> TryRehydrate(Guid sourceId);
    }
}
