namespace Loom.Messaging
{
    using System;
    using System.Collections.Generic;

    [Obsolete("Use ActivityError class instead.")]
    public class HandlerError
    {
        // TODO: Remove guard clauses after apply C# 8.0.
        public HandlerError(string code,
                            string message,
                            string stackTrace,
                            params HandlerError[] details)
        {
            if (details == null)
            {
                throw new ArgumentNullException(nameof(details));
            }

            foreach (HandlerError detail in details)
            {
                if (detail == null)
                {
                    throw new ArgumentException("Value should not contain null element.", nameof(details));
                }
            }

            Code = code ?? throw new ArgumentNullException(nameof(code));
            Message = message ?? throw new ArgumentNullException(nameof(message));
            StackTrace = stackTrace ?? throw new ArgumentNullException(nameof(stackTrace));
            Details = new List<HandlerError>(details).AsReadOnly();
        }

        public string Code { get; }

        public string Message { get; }

        public string StackTrace { get; }

        public IEnumerable<HandlerError> Details { get; }
    }
}
