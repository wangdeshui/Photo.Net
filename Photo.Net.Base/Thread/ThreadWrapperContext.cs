using System;
using System.Collections;
using System.Threading;

namespace Photo.Net.Base.Thread
{
    /// <summary>
    /// A thread context
    /// </summary>
    internal sealed class ThreadWrapperContext
    {
        private readonly WaitCallback _callback;
        private readonly object _context;
        private readonly IDisposable _counterToken;
        private readonly ArrayList _exceptionsBucket;

        public ThreadWrapperContext(WaitCallback callback, object context,
            IDisposable counterToken, ArrayList exceptionsBucket)
        {
            _callback = callback;
            _context = context;
            _counterToken = counterToken;
            _exceptionsBucket = exceptionsBucket;
        }

        public void Run()
        {
            using (_counterToken)
            {
                try
                {
                    _callback(this._context);
                }

                catch (Exception ex)
                {
                    _exceptionsBucket.Add(ex);
                }
            }
        }

        public void Run(object state)
        {
            Run();
        }
    }
}
