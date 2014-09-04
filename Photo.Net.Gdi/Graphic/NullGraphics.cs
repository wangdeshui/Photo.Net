using System;
using Photo.Net.Base;
using Photo.Net.Base.Native;

namespace Photo.Net.Gdi.Graphic
{
    /// <summary>
    /// Sometimes you need a Graphics instance when you don't really have access to one.
    /// Example situations include retrieving the bounds or scanlines of a Region.
    /// So use this to create a 'null' Graphics instance that effectively eats all
    /// rendering calls.
    /// </summary>
    public sealed class NullGraphics
        : IDisposable
    {
        private readonly IntPtr _hdc = IntPtr.Zero;
        private System.Drawing.Graphics _graphics;
        private bool _disposed;

        public System.Drawing.Graphics Graphics
        {
            get
            {
                return _graphics;
            }
        }

        public NullGraphics()
        {
            this._hdc = SafeNativeMethods.CreateCompatibleDC(IntPtr.Zero);

            if (this._hdc == IntPtr.Zero)
            {
                NativeMethods.ThrowOnWin32Error("CreateCompatibleDC returned NULL");
            }

            this._graphics = System.Drawing.Graphics.FromHdc(this._hdc);
        }

        ~NullGraphics()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    this._graphics.Dispose();
                    this._graphics = null;
                }

                SafeNativeMethods.DeleteDC(this._hdc);
                _disposed = true;
            }
        }
    }
}
