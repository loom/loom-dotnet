namespace Loom.EventSourcing.EntityFrameworkCore
{
    using System.Collections.Generic;

    public interface IUniquePropertyDetector
    {
        IReadOnlyDictionary<string, string> GetUniqueProperties(object source);
    }
}
