namespace Loom.EventSourcing.EntityFrameworkCore
{
    public interface IState
    {
        public string StreamId { get; }

        public long Version { get; }
    }
}
