namespace Loom.Messaging.Processes
{
    using System;

    public sealed class ProcessOptionsBuilder
    {
        private Func<object, bool> _completionDeterminer;
        private TimeSpan _timeout;

        public ProcessOptionsBuilder()
        {
            _completionDeterminer = _ => false;
            _timeout = TimeSpan.FromMinutes(1);
        }

        public ProcessOptionsBuilder WaitFor<T>()
        {
            Func<object, bool> inner = _completionDeterminer;
            _completionDeterminer = data => inner.Invoke(data) || data is T;
            return this;
        }

        public ProcessOptionsBuilder WithTimeout(TimeSpan timeout)
        {
            _timeout = timeout;
            return this;
        }

        public ProcessOptions Build()
            => new ProcessOptions(_completionDeterminer, _timeout);
    }
}
