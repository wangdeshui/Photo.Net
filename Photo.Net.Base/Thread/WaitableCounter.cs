using System;
using System.Threading;

namespace Photo.Net.Base.Thread
{
    /// <summary>
    /// Threading primitive that allows you to "count" and to wait on two conditions:
    /// 1. Empty -- this is when we have not dished out any "tokens"
    /// 2. NotFull -- this is when we currently have 1 or more "tokens" out in the wild
    /// Note that the tokens given by Acquire() *must* be disposed. Otherwise things
    /// won't work right!
    /// </summary>
    public class WaitableCounter
    {
        /// <summary>
        /// The minimum value that may be passed to the constructor for initialization.
        /// </summary>
        public static int MinimumCount
        {
            get
            {
                return WaitHandleArray.MinimumCount;
            }
        }

        /// <summary>
        /// The maximum value that may be passed to the construct for initialization.
        /// </summary>
        public static int MaximumCount
        {
            get
            {
                return WaitHandleArray.MaximumCount;
            }
        }

        private readonly WaitHandleArray _freeEvents;    // each of these is signaled (set) when the corresponding slot is 'free'
        private readonly WaitHandleArray _inUseEvents;   // each of these is signaled (set) when the corresponding slot is 'in use'

        private readonly object _theLock;

        public WaitableCounter(int maxCount)
        {
            if (maxCount < 1 || maxCount > 64)
            {
                throw new ArgumentOutOfRangeException("maxCount", "must be between 1 and 64, inclusive");
            }

            this._freeEvents = new WaitHandleArray(maxCount);
            this._inUseEvents = new WaitHandleArray(maxCount);

            for (int i = 0; i < maxCount; ++i)
            {
                this._freeEvents[i] = new ManualResetEvent(true);
                this._inUseEvents[i] = new ManualResetEvent(false);
            }

            this._theLock = new object();
        }

        public void Release(CounterToken token)
        {
            ((ManualResetEvent)this._inUseEvents[token.Index]).Reset();
            ((ManualResetEvent)this._freeEvents[token.Index]).Set();
        }

        public IDisposable AcceptQuireToken()
        {
            lock (this._theLock)
            {
                int index = WaitForNotFull();
                ((ManualResetEvent)this._freeEvents[index]).Reset();
                ((ManualResetEvent)this._inUseEvents[index]).Set();
                return new CounterToken(this, index);
            }
        }

        public bool IsEmpty()
        {
            return IsEmpty(0);
        }

        public bool IsEmpty(uint msTimeout)
        {
            return _freeEvents.AreAllSignaled(msTimeout);
        }

        public void WaitForEmpty()
        {
            _freeEvents.WaitAll();
        }

        public int WaitForNotFull()
        {
            int returnVal = _freeEvents.WaitAny();
            return returnVal;
        }

    }

    public sealed class CounterToken
        : IDisposable
    {
        public int Index { get; private set; }
        private readonly WaitableCounter _parent;

        public CounterToken(WaitableCounter parent, int index)
        {
            this._parent = parent;
            this.Index = index;
        }

        public void Dispose()
        {
            _parent.Release(this);
        }
    }
}
