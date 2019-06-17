namespace Loom.Messaging
{
    using System;
    using System.Collections.Generic;

    public readonly struct TracingProperties : IEquatable<TracingProperties>
    {
        // TODO: Change the type of parameter 'operationId' to string?.
        // TODO: Change the type of parameter 'contributor' to string?.
        // TODO: Change the type of parameter 'parentId' to string?.
        public TracingProperties(string operationId, string contributor, string parentId)
        {
            OperationId = operationId;
            Contributor = contributor;
            ParentId = parentId;
        }

        // TODO: Change the type to string?.
        public string OperationId { get; }

        // TODO: Change the type to string?.
        public string Contributor { get; }

        // TODO: Change the type to string?.
        public string ParentId { get; }

        public static bool operator ==(TracingProperties left, TracingProperties right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TracingProperties left, TracingProperties right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
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
            EqualityComparer<string> comparer = EqualityComparer<string>.Default;
            hashCode = (hashCode * -1521134295) + comparer.GetHashCode(OperationId);
            hashCode = (hashCode * -1521134295) + comparer.GetHashCode(Contributor);
            hashCode = (hashCode * -1521134295) + comparer.GetHashCode(ParentId);
            return hashCode;
        }
    }
}
