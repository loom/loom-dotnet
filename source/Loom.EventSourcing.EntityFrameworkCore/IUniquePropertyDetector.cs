using System.Collections.Generic;

namespace Loom.EventSourcing.EntityFrameworkCore
{
    public interface IUniquePropertyDetector
    {
        IReadOnlyDictionary<string, string> GetUniqueProperties(object source);
    }
}
