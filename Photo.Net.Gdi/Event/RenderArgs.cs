using System;
using System.Drawing;
using Photo.Net.Core;

namespace Photo.Net.Gdi.Event
{
    /// <summary>
    /// Encapsulates the arguments passed to a Render function.
    /// This way we can do on-demand and once-only creation of Bitmap and Graphics
    /// objects from a given Surface object.
    /// </summary>
    /// <remarks>
    /// Use of the Bitmap and Graphics objects is not thread safe because of how GDI+ works.
    /// You must wrap use of these objects with a critical section, like so:
    ///     object lockObject = new object();
    ///     lock (lockObject)
    ///     {
    ///         Graphics g = ra.Graphics;
    ///         g.DrawRectangle(...);
    ///         // etc.
    ///     }
    /// </remarks>
    public sealed class RenderArgs
        : IDisposable
    {
        private readonly Surface _surface;
        private Bitmap _bitmap;
        private Graphics _graphics;
        private bool _disposed;

        public Surface Surface
        {
            get
            {
                CheckDispose();
                return this._surface;
            }
        }

        public Bitmap Bitmap
        {
            get
            {
                CheckDispose();
                return this._bitmap ?? (this._bitmap = _surface.CreateAliasedBitmap());
            }
        }

        public Graphics Graphics
        {
            get
            {
                CheckDispose();
                return this._graphics ?? (this._graphics = Graphics.FromImage(Bitmap));
            }
        }

        public Rectangle Bounds
        {
            get
            {
                CheckDispose();
                return this.Surface.Bounds;
            }
        }

        public Size Size
        {
            get
            {
                CheckDispose();
                return this.Surface.Size;
            }
        }

        public int Width
        {
            get
            {
                CheckDispose();
                return this._surface.Width;
            }
        }

        public int Height
        {
            get
            {
                CheckDispose();
                return this._surface.Height;
            }
        }

        public RenderArgs(Surface surface)
        {
            this._surface = surface;
            this._bitmap = null;
            this._graphics = null;
        }

        ~RenderArgs()
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
            if (!this._disposed)
            {
                this._disposed = true;

                if (disposing)
                {
                    if (this._graphics != null)
                    {
                        this._graphics.Dispose();
                        this._graphics = null;
                    }

                    if (this._bitmap != null)
                    {
                        this._bitmap.Dispose();
                        this._bitmap = null;
                    }
                }
            }
        }

        private void CheckDispose()
        {
            if (this._disposed)
            {
                throw new ObjectDisposedException("RenderArgs");
            }
        }
    }
}