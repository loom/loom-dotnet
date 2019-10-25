namespace Loom.Messaging
{
    using System;
    using System.Collections.Generic;

    public readonly struct TracingProperties : IEquatable<TracingProperties>
    {
        public TracingProperties(string operationId, string? contributor, string? parentId)
        {
            OperationId = operationId;
            Contributor = contributor;
            ParentId = parentId;
        }

        public string OperationId { get; }

        public string? Contributor { get; }

        public string? ParentId { get; }

        public static bool operator ==(TracingProperties left, TracingProperties right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TracingProperties left, TracingProperties right)
        {
            return !(left == right);
        }

        public override bool Equals(object? obj)
        {
            return obj is TracingProperties properties && Equals(properties);
        }

        public bool Equals(TracingProperties other)
        {
            return OperationId == other.OperationId &&
                   Contributor == other.Contributor &&
                   ParentId == other.ParentId;
        }

        public override int GetHashCode()
        {
            int hashCode = 147070445;
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(OperationId);
            hashCode = (hashCode * -1521134295) + EqualityComparer<string?>.Default.GetHashCode(Contributor);
            hashCode = (hashCode * -1521134295) + EqualityComparer<string?>.Default.GetHashCode(ParentId);
            return hashCode;
        }
    }
}
