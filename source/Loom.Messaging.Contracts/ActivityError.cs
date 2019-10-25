namespace Loom.Messaging
{
    using System.Collections.Generic;

    public sealed class ActivityError
    {
        public ActivityError(string code,
                             string message,
                             string? stackTrace,
                             params ActivityError[] details)
        {
            Code = code;
            Message = message;
            StackTrace = stackTrace;
            Details = new List<ActivityError>(details).AsReadOnly();
        }

        public string Code { get; }

        public string Message { get; }

        public string? StackTrace { get; }

        public IEnumerable<ActivityError> Details { get; }
    }
}
