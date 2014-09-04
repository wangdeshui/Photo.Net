using System;

namespace Photo.Net.Base.Exceptions
{
    /// <summary>
    /// This exception is thrown by a foreground thread when a background worker thread
    /// had an exception. This allows all exceptions to be handled by the foreground thread.
    /// </summary>
    public class WorkerThreadException
        : Exception
    {
        private const string defaultMessage = "Worker thread threw an exception";

        public WorkerThreadException(Exception innerException)
            : this(defaultMessage, innerException)
        {
        }

        public WorkerThreadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
