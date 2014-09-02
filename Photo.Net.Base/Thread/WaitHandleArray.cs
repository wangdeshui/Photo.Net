/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading;
using Photo.Net.Base.NativeWrapper;

namespace Photo.Net.Base.Thread
{
    /// <summary>
    /// Encapsulates an array of WaitHandles and methods for waiting on them.
    /// This class does not take ownership of the WaitHandles; you must still
    /// Dispose() them yourself.
    /// </summary>
    /// <remarks>
    /// This class exists because System.Threading.WaitHandle.Wait[Any|All] will throw an exception
    /// in an STA apartment. So we must P/Invoke down to WaitForMultipleObjects().
    /// </remarks>
    public sealed class WaitHandleArray
    {
        private readonly WaitHandle[] _waitHandles;
        private readonly IntPtr[] _nativeHandles;

        /// <summary>
        /// The minimum value that may be passed to the constructor for initialization.
        /// </summary>
        public const int MinimumCount = 1;

        /// <summary>
        /// The maximum value that may be passed to the construct for initialization.
        /// </summary>
        public const int MaximumCount = 64;

        /// <summary>
        /// Gets or sets the WaitHandle at the specified index.
        /// </summary>
        public WaitHandle this[int index]
        {
            get
            {
                return this._waitHandles[index];
            }

            set
            {
                this._waitHandles[index] = value;
                this._nativeHandles[index] = value.SafeWaitHandle.DangerousGetHandle();
            }
        }

        /// <summary>
        /// Gets the length of the array.
        /// </summary>
        public int Length
        {
            get
            {
                return this._waitHandles.Length;
            }
        }

        /// <summary>
        /// Initializes a new instance of the WaitHandleArray class.
        /// </summary>
        /// <param name="count">The size of the array.</param>
        public WaitHandleArray(int count)
        {
            if (count < 1 || count > 64)
            {
                throw new ArgumentOutOfRangeException("count", "must be between 1 and 64, inclusive");
            }

            this._waitHandles = new WaitHandle[count];
            this._nativeHandles = new IntPtr[count];
        }

        private uint WaitForAll(uint dwTimeout)
        {
            return SafeNativeMethods.WaitForMultipleObjects(this._nativeHandles, true, dwTimeout);
        }

        /// <summary>
        /// Waits for all of the WaitHandles to be signaled.
        /// </summary>
        public void WaitAll()
        {
            WaitForAll(NativeConstants.INFINITE);
        }

        public bool AreAllSignaled()
        {
            return AreAllSignaled(0);
        }

        public bool AreAllSignaled(uint msTimeout)
        {
            uint result = WaitForAll(msTimeout);

            return result < (NativeConstants.WAIT_OBJECT_0 + this.Length);
        }

        /// <summary>
        /// Waits for any of the WaitHandles to be signaled.
        /// </summary>
        /// <returns>
        /// The index of the first item in the array that completed the wait operation.
        /// If this value is outside the bounds of the array, it is an indication of an
        /// error.
        /// </returns>
        public int WaitAny()
        {
            int returnVal = (int)SafeNativeMethods.WaitForMultipleObjects(this._nativeHandles, false, NativeConstants.INFINITE);
            return returnVal;
        }
    }
}
