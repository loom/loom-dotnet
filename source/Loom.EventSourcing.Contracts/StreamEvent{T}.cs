using System;

namespace Loom.EventSourcing
{
    public sealed record StreamEvent<T>(
        string StreamId,
        long Version,
        DateTime RaisedTimeUtc,
        T Payload);
}
