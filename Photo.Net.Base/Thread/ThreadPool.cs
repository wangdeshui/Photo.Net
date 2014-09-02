using System;
using System.Collections;
using System.Threading;
using Photo.Net.Base.Infomation;

namespace Photo.Net.Base.Thread
{
    /// <summary>
    /// Uses the .NET ThreadPool to do our own type of thread pool. The main difference
    /// here is that we limit our usage of the thread pool.
    /// The default maximum number of threads is
    /// Processor.LogicalCpuCount.
    /// </summary>
    public class ThreadPool
    {
        /// <summary>
        /// This is a global thread pool.
        /// </summary>
        public static ThreadPool Global = new ThreadPool(2 * Processor.LogicalCpuCount);

        private readonly ArrayList _exceptions = ArrayList.Synchronized(new ArrayList());
        private readonly bool _useClrTheadPool;

        public static int MinimumCount
        {
            get
            {
                return WaitableCounter.MinimumCount;
            }
        }

        public static int MaximumCount
        {
            get
            {
                return WaitableCounter.MaximumCount;
            }
        }

        public Exception[] Exceptions
        {
            get
            {
                return (Exception[])this._exceptions.ToArray(typeof(Exception));
            }
        }

        public void ClearExceptions()
        {
            _exceptions.Clear();
        }

        public void DrainExceptions()
        {
            if (this._exceptions.Count > 0)
            {
                throw new ThreadInterruptedException("Worker thread threw an exception", (Exception)this._exceptions[0]);
            }

            ClearExceptions();
        }

        private readonly WaitableCounter _counter;

        public ThreadPool()
            : this(Processor.LogicalCpuCount)
        {
        }

        public ThreadPool(int maxThreads)
            : this(maxThreads, true)
        {
        }

        public ThreadPool(int maxThreads, bool useClrTheadPool)
        {
            if (maxThreads < MinimumCount || maxThreads > MaximumCount)
            {
                throw new ArgumentOutOfRangeException("maxThreads", "must be between " + MinimumCount + " and " + MaximumCount + " inclusive");
            }

            _counter = new WaitableCounter(maxThreads);
            _useClrTheadPool = useClrTheadPool;
        }

        public void QueueTask(WaitCallback callback)
        {
            QueueTask(callback, null);
        }

        public void QueueTask(WaitCallback callback, object state)
        {
            IDisposable token = _counter.AcceptQuireToken();
            var context = new ThreadWrapperContext(callback, state, token, _exceptions);

            if (_useClrTheadPool)
            {
                System.Threading.ThreadPool.QueueUserWorkItem(context.Run, context);
            }
            else
            {
                var thread = new System.Threading.Thread(new ThreadStart(context.Run)) { IsBackground = true };
                thread.Start();
            }
        }

        public bool IsDrained(uint msTimeout)
        {
            bool result = _counter.IsEmpty(msTimeout);

            if (result)
            {
                Drain();
            }

            return result;
        }

        public bool IsDrained()
        {
            return IsDrained(0);
        }

        public void Drain()
        {
            _counter.WaitForEmpty();
            DrainExceptions();
        }

    }
}