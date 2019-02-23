namespace Loom.EventSourcing
{
    using System;
    using System.Threading.Tasks;

    public interface ISnapshotter
    {
        Task TakeSnapshot(Guid sourceId);
    }
}
