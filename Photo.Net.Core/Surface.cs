using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using Photo.Net.Base;
using Photo.Net.Core.Color;
using Photo.Net.Core.Geometry;
using Photo.Net.Core.PixelOperation;

namespace Photo.Net.Core
{
    /// <summary>
    /// This is our Surface type. We allocate our own blocks of memory for this,
    /// and provide ways to create a GDI+ Bitmap object that aliases our surface.
    /// That way we can do everything fast, in memory and have complete control,
    /// and still have the ability to use GDI+ for drawing and rendering where
    /// appropriate.
    /// </summary>
    [Serializable]
    public sealed class Surface
        : IDisposable,
          ICloneable
    {
        #region Property

        private MemoryBlock _scan0;
        private int _width;
        private int _height;
        private int _stride;
        private bool _disposed;

        public bool IsDisposed
        {
            get { return this._disposed; }
        }

        /// <summary>
        /// Gets a MemoryBlock which is the buffer holding the pixels associated
        /// with this Surface.
        /// </summary>
        public MemoryBlock Scan0
        {
            get
            {
                if (this._disposed)
                {
                    throw new ObjectDisposedException("Surface");
                }

                return this._scan0;
            }
        }

        /// <summary>
        /// Gets the width, in pixels, of this Surface.
        /// </summary>
        /// <remarks>
        public int Width
        {
            get { return this._width; }
        }

        /// <summary>
        /// Gets the height, in pixels, of this Surface.
        /// </summary>
        /// <remarks>
        public int Height
        {
            get { return this._height; }
        }

        /// <summary>
        /// Gets the stride, in bytes, for this Surface.
        /// </summary>
        /// <remarks>
        /// Stride is defined as the number of bytes between the beginning of a row and
        /// the beginning of the next row. Thus, in loose C notation: stride = (byte *)&this[0, 1] - (byte *)&this[0, 0].
        /// Stride will always be equal to <b>or greater than</b> Width * ColorBgra.Size.
        public int Stride
        {
            get { return this._stride; }
        }

        /// <summary>
        /// Gets the size, in pixels, of this Surface.
        /// </summary>
        /// <remarks>
        /// This is a convenience function that creates a new Size instance based
        /// on the values of the Width and Height properties.
        /// This property will never throw an ObjectDisposedException.
        /// </remarks>
        public Size Size
        {
            get { return new Size(this._width, this._height); }
        }

        /// <summary>
        /// Gets the GDI+ PixelFormat of this Surface.
        /// </summary>
        /// <remarks>
        /// This property always returns PixelFormat.Format32bppArgb.
        /// This property will never throw an ObjectDisposedException.
        /// </remarks>
        public PixelFormat PixelFormat
        {
            get { return PixelFormat.Format32bppArgb; }
        }

        /// <summary>
        /// Gets the bounds of this Surface, in pixels.
        /// </summary>
        /// <remarks>
        /// This is a convenience function that returns Rectangle(0, 0, Width, Height).
        /// This property will never throw an ObjectDisposedException.
        /// </remarks>
        public Rectangle Bounds
        {
            get { return new Rectangle(0, 0, _width, _height); }
        }

        #endregion

        #region Check

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("Surface");
            }
        }

        private void CheckBound(int x, int y)
        {
            if (!IsVisible(x, y))
            {
                throw new ArgumentOutOfRangeException("(x,y)", new Point(x, y), "Coordinates out of range, max=" + new Size(_width - 1, _height - 1));
            }
        }

        public bool IsVisible(int x, int y)
        {
            return x >= 0 && x < _width && y >= 0 && y < _height;
        }

        public bool IsVisible(Point pt)
        {
            return IsVisible(pt.X, pt.Y);
        }

        public bool IsRowVisible(int y)
        {
            return y >= 0 && y < Height;
        }

        public bool IsColumnVisible(int x)
        {
            return x >= 0 && x < Width;
        }

        #endregion

        #region Build

        /// <summary>
        /// Creates a new instance of the Surface class.
        /// </summary>
        /// <param name="size">The size, in pixels, of the new Surface.</param>
        public Surface(Size size)
            : this(size.Width, size.Height)
        {
        }

        /// <summary>
        /// Creates a new instance of the Surface class.
        /// </summary>
        /// <param name="width">The width, in pixels, of the new Surface.</param>
        /// <param name="height">The height, in pixels, of the new Surface.</param>
        public Surface(int width, int height)
        {
            int stride;
            long bytes;

            try
            {
                stride = checked(width * ColorBgra.Size);
                bytes = (long)height * (long)stride;
            }

            catch (OverflowException ex)
            {
                throw new OutOfMemoryException(
                    "Dimensions are too large - not enough memory, width=" + width.ToString() + ", height=" +
                    height.ToString(), ex);
            }

            MemoryBlock scan0 = new MemoryBlock(width, height);
            Create(width, height, stride, scan0);
        }

        /// <summary>
        /// Creates a new instance of the Surface class that reuses a block of memory that was previously allocated.
        /// </summary>
        /// <param name="width">The width, in pixels, for the Surface.</param>
        /// <param name="height">The height, in pixels, for the Surface.</param>
        /// <param name="stride">The stride, in bytes, for the Surface.</param>
        /// <param name="scan0">The MemoryBlock to use. The beginning of this buffer defines the upper left (0, 0) pixel of the Surface.</param>
        private Surface(int width, int height, int stride, MemoryBlock scan0)
        {
            Create(width, height, stride, scan0);
        }

        private void Create(int width, int height, int stride, MemoryBlock scan0)
        {
            this._width = width;
            this._height = height;
            this._stride = stride;
            this._scan0 = scan0;
        }

        ~Surface()
        {
            Dispose(false);
        }

        /// <summary>
        /// Creates a Surface that aliases a portion of this Surface.
        /// </summary>
        /// <param name="bounds">The portion of this Surface that will be aliased.</param>
        /// <remarks>The upper left corner of the new Surface will correspond to the 
        /// upper left corner of this rectangle in the original Surface.</remarks>
        /// <returns>A Surface that aliases the requested portion of this Surface.</returns>
        public Surface CreateWindow(Rectangle bounds)
        {
            return CreateWindow(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }

        public Surface CreateWindow(int x, int y, int windowWidth, int windowHeight)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("Surface");
            }

            if (windowHeight == 0)
            {
                throw new ArgumentOutOfRangeException("windowHeight", "must be greater than zero");
            }

            Rectangle original = this.Bounds;
            Rectangle sub = new Rectangle(x, y, windowWidth, windowHeight);
            Rectangle clipped = Rectangle.Intersect(original, sub);

            if (clipped != sub)
            {
                throw new ArgumentOutOfRangeException("bounds", new Rectangle(x, y, windowWidth, windowHeight),
                    "bounds parameters must be a subset of this Surface's bounds");
            }

            long offset = ((long)_stride * (long)y) + ((long)ColorBgra.Size * (long)x);
            long length = ((windowHeight - 1) * (long)_stride) + (long)windowWidth * (long)ColorBgra.Size;
            MemoryBlock block = new MemoryBlock(this._scan0, offset, length);
            return new Surface(windowWidth, windowHeight, this._stride, block);
        }

        #endregion

        #region Get pixel infomation

        public ColorBgra this[int x, int y]
        {
            get
            {
                CheckDisposed();
                CheckBound(x, y);

                unsafe
                {
                    return *UnsafeGetPointAddress(x, y);
                }
            }

            set
            {
                CheckDisposed();
                CheckBound(x, y);

                unsafe
                {
                    *UnsafeGetPointAddress(x, y) = value;
                }
            }
        }

        public ColorBgra this[Point pt]
        {
            get
            {
                return this[pt.X, pt.Y];
            }

            set
            {
                this[pt.X, pt.Y] = value;
            }
        }

        public long GetRowByteOffset(int y)
        {
            if (y < 0 || y >= _height)
            {
                throw new ArgumentOutOfRangeException("y", "Out of bounds: y=" + y.ToString());
            }

            return (long)y * _stride;
        }

        public long UnsafeGetRowByteOffset(int y)
        {
            return (long)y * _stride;
        }

        public unsafe ColorBgra* GetRowAddress(int y)
        {
            return (ColorBgra*)(((byte*)_scan0.VoidStar) + GetRowByteOffset(y));
        }

        public unsafe ColorBgra* UnsafeGetRowAddress(int y)
        {
            return (ColorBgra*)(((byte*)_scan0.VoidStar) + UnsafeGetRowByteOffset(y));
        }

        public long GetColumnByteOffset(int x)
        {
            if (x < 0 || x >= this._width)
            {
                throw new ArgumentOutOfRangeException("x", x, "Out of bounds");
            }

            return (long)x * ColorBgra.Size;
        }

        public long UnsafeGetColumnByteOffset(int x)
        {
            return (long)x * ColorBgra.Size;
        }

        public long GetPointByteOffset(int x, int y)
        {
            return GetRowByteOffset(y) + GetColumnByteOffset(x);
        }

        public long UnsafeGetPointByteOffset(int x, int y)
        {
            return UnsafeGetRowByteOffset(y) + UnsafeGetColumnByteOffset(x);
        }

        public ColorBgra GetPoint(int x, int y)
        {
            return this[x, y];
        }

        public unsafe ColorBgra UnsafeGetPoint(int x, int y)
        {
            return *(x + (ColorBgra*)(((byte*)_scan0.VoidStar) + (y * _stride)));
        }

        public ColorBgra UnsafeGetPoint(Point pt)
        {
            return UnsafeGetPoint(pt.X, pt.Y);
        }

        public unsafe ColorBgra* GetPointAddress(int x, int y)
        {
            if (x < 0 || x >= Width)
            {
                throw new ArgumentOutOfRangeException("x", "Out of bounds: x=" + x.ToString());
            }

            return GetRowAddress(y) + x;
        }

        public unsafe ColorBgra* GetPointAddress(Point pt)
        {
            return GetPointAddress(pt.X, pt.Y);
        }

        public unsafe ColorBgra* UnsafeGetPointAddress(int x, int y)
        {
            return unchecked(x + (ColorBgra*)(((byte*)_scan0.VoidStar) + (y * _stride)));
        }

        public unsafe ColorBgra* UnsafeGetPointAddress(Point pt)
        {
            return UnsafeGetPointAddress(pt.X, pt.Y);
        }

        #endregion

        /// <summary>
        /// Gets a MemoryBlock that references the row requested.
        /// </summary>
        public MemoryBlock GetRow(int y)
        {
            return new MemoryBlock(_scan0, GetRowByteOffset(y), (long)_width * ColorBgra.Size);
        }

        public bool IsContiguousMemoryRegion(Rectangle bounds)
        {
            bool oneRow = (bounds.Height == 1);
            bool manyRows = (this.Stride == (this.Width * ColorBgra.Size) &&
                this.Width == bounds.Width);

            return oneRow || manyRows;
        }

        #region Get color

        [Obsolete("Use GetBilinearSampleWrapped(float, float) instead")]
        public ColorBgra GetBilinearSample(float x, float y, bool wrap)
        {
            return GetBilinearSampleWrapped(x, y);
        }

        public ColorBgra GetBilinearSampleWrapped(float x, float y)
        {
            if (!Utility.IsNumber(x) || !Utility.IsNumber(y))
            {
                return ColorBgra.Transparent;
            }

            float u = x;
            float v = y;

            unchecked
            {
                var iu = (int)Math.Floor(u);
                var sxfrac = (uint)(256 * (u - iu));
                uint sxfracinv = 256 - sxfrac;

                var iv = (int)Math.Floor(v);
                var syfrac = (uint)(256 * (v - iv));
                uint syfracinv = 256 - syfrac;

                var wul = sxfracinv * syfracinv;
                uint wur = sxfrac * syfracinv;
                var wll = sxfracinv * syfrac;
                uint wlr = sxfrac * syfrac;

                int sx = iu;
                if (sx < 0)
                {
                    sx = (_width - 1) + ((sx + 1) % _width);
                }
                else if (sx > (_width - 1))
                {
                    sx = sx % _width;
                }

                int sy = iv;
                if (sy < 0)
                {
                    sy = (_height - 1) + ((sy + 1) % _height);
                }
                else if (sy > (_height - 1))
                {
                    sy = sy % _height;
                }

                int sleft = sx;
                int sright;

                if (sleft == (_width - 1))
                {
                    sright = 0;
                }
                else
                {
                    sright = sleft + 1;
                }

                int stop = sy;
                int sbottom;

                if (stop == (_height - 1))
                {
                    sbottom = 0;
                }
                else
                {
                    sbottom = stop + 1;
                }

                ColorBgra cul = UnsafeGetPoint(sleft, stop);
                ColorBgra cur = UnsafeGetPoint(sright, stop);
                ColorBgra cll = UnsafeGetPoint(sleft, sbottom);
                ColorBgra clr = UnsafeGetPoint(sright, sbottom);

                ColorBgra c = ColorBgra.BlendColors4W16IP(cul, wul, cur, wur, cll, wll, clr, wlr);

                return c;
            }
        }

        [Obsolete("Use GetBilinearSample(float, float) instead")]
        public ColorBgra GetBilinearSample2(float x, float y)
        {
            return GetBilinearSample(x, y);
        }

        public unsafe ColorBgra GetBilinearSample(float x, float y)
        {
            if (!Utility.IsNumber(x) || !Utility.IsNumber(y))
            {
                return ColorBgra.Transparent;
            }

            float u = x;
            float v = y;

            if (u >= 0 && v >= 0 && u < _width && v < _height)
            {
                unchecked
                {
                    int iu = (int)Math.Floor(u);
                    uint sxfrac = (uint)(256 * (u - (float)iu));
                    uint sxfracinv = 256 - sxfrac;

                    int iv = (int)Math.Floor(v);
                    uint syfrac = (uint)(256 * (v - (float)iv));
                    uint syfracinv = 256 - syfrac;

                    uint wul = (uint)(sxfracinv * syfracinv);
                    uint wur = (uint)(sxfrac * syfracinv);
                    uint wll = (uint)(sxfracinv * syfrac);
                    uint wlr = (uint)(sxfrac * syfrac);

                    int sx = iu;
                    int sy = iv;
                    int sleft = sx;
                    int sright;

                    if (sleft == (_width - 1))
                    {
                        sright = sleft;
                    }
                    else
                    {
                        sright = sleft + 1;
                    }

                    int stop = sy;
                    int sbottom;

                    if (stop == (_height - 1))
                    {
                        sbottom = stop;
                    }
                    else
                    {
                        sbottom = stop + 1;
                    }

                    ColorBgra* cul = UnsafeGetPointAddress(sleft, stop);
                    ColorBgra* cur = cul + (sright - sleft);
                    ColorBgra* cll = UnsafeGetPointAddress(sleft, sbottom);
                    ColorBgra* clr = cll + (sright - sleft);

                    ColorBgra c = ColorBgra.BlendColors4W16IP(*cul, wul, *cur, wur, *cll, wll, *clr, wlr);
                    return c;
                }
            }
            else
            {
                return ColorBgra.FromUInt32(0);
            }
        }

        [Obsolete("Use GetBilinearSampleClamped(float, float) instead")]
        public ColorBgra GetBilinearSample2Clamped(float x, float y)
        {
            return GetBilinearSampleClamped(x, y);
        }

        public unsafe ColorBgra GetBilinearSampleClamped(float x, float y)
        {
            if (!Utility.IsNumber(x) || !Utility.IsNumber(y))
            {
                return ColorBgra.Transparent;
            }

            float u = x;
            float v = y;

            if (u < 0)
            {
                u = 0;
            }
            else if (u > this.Width - 1)
            {
                u = this.Width - 1;
            }

            if (v < 0)
            {
                v = 0;
            }
            else if (v > this.Height - 1)
            {
                v = this.Height - 1;
            }

            unchecked
            {
                int iu = (int)Math.Floor(u);
                uint sxfrac = (uint)(256 * (u - (float)iu));
                uint sxfracinv = 256 - sxfrac;

                int iv = (int)Math.Floor(v);
                uint syfrac = (uint)(256 * (v - (float)iv));
                uint syfracinv = 256 - syfrac;

                uint wul = (uint)(sxfracinv * syfracinv);
                uint wur = (uint)(sxfrac * syfracinv);
                uint wll = (uint)(sxfracinv * syfrac);
                uint wlr = (uint)(sxfrac * syfrac);

                int sx = iu;
                int sy = iv;
                int sleft = sx;
                int sright;

                if (sleft == (_width - 1))
                {
                    sright = sleft;
                }
                else
                {
                    sright = sleft + 1;
                }

                int stop = sy;
                int sbottom;

                if (stop == (_height - 1))
                {
                    sbottom = stop;
                }
                else
                {
                    sbottom = stop + 1;
                }

                ColorBgra* cul = UnsafeGetPointAddress(sleft, stop);
                ColorBgra* cur = cul + (sright - sleft);
                ColorBgra* cll = UnsafeGetPointAddress(sleft, sbottom);
                ColorBgra* clr = cll + (sright - sleft);

                ColorBgra c = ColorBgra.BlendColors4W16IP(*cul, wul, *cur, wur, *cll, wll, *clr, wlr);
                return c;
            }
        }

        #endregion

        #region Create bitmap

        public Bitmap CreateAliasedBitmap()
        {
            return CreateAliasedBitmap(this.Bounds);
        }

        public Bitmap CreateAliasedBitmap(Rectangle bounds)
        {
            return CreateAliasedBitmap(bounds, true);
        }

        /// <summary>
        /// Create a bitmap, it size equal the special rectangle intersect with current surface's bound
        /// </summary>
        /// <param name="bounds">bitmap rectangle</param>
        /// <param name="alpha">has alpha channel</param>
        public Bitmap CreateAliasedBitmap(Rectangle bounds, bool alpha)
        {
            CheckDisposed();

            if (bounds.IsEmpty)
            {
                throw new ArgumentOutOfRangeException();
            }

            Rectangle clipped = Rectangle.Intersect(this.Bounds, bounds);

            if (clipped != bounds)
            {
                throw new ArgumentOutOfRangeException();
            }

            unsafe
            {
                return new Bitmap(bounds.Width, bounds.Height, _stride,
                    alpha ? this.PixelFormat : PixelFormat.Format32bppRgb,
                    new IntPtr((byte*)_scan0.VoidStar + UnsafeGetPointByteOffset(bounds.X, bounds.Y)));
            }
        }

        public void GetDrawBitmapInfo(out IntPtr bitmapHandle, out Point childOffset, out Size parentSize)
        {
            MemoryBlock rootBlock = _scan0.GetRootMemoryBlock();
            long childOffsetBytes = this._scan0.Pointer.ToInt64() - rootBlock.Pointer.ToInt64();
            var childY = (int)(childOffsetBytes / this._stride);
            var childX = (int)((childOffsetBytes - (childY * this._stride)) / ColorBgra.Size);

            childOffset = new Point(childX, childY);
            parentSize = new Size(_stride / ColorBgra.Size, childY + _height);
            bitmapHandle = rootBlock.BitmapHandle;
        }

        #endregion

        #region Clone and copy


        /// <summary>
        /// Creates a new Surface and copies the pixels from a Bitmap to it.
        /// </summary>
        /// <param name="bitmap">The Bitmap to duplicate.</param>
        /// <returns>A new Surface that is the same size as the given Bitmap and that has the same pixel values.</returns>
        public static Surface CopyFromBitmap(Bitmap bitmap)
        {
            Surface surface = new Surface(bitmap.Width, bitmap.Height);
            BitmapData bd = bitmap.LockBits(surface.Bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            unsafe
            {
                for (int y = 0; y < bd.Height; ++y)
                {
                    Memory.Copy(surface.GetRowAddress(y),
                        (byte*)bd.Scan0.ToPointer() + (y * bd.Stride), (ulong)bd.Width * ColorBgra.Size);
                }
            }

            bitmap.UnlockBits(bd);
            return surface;
        }

        /// <summary>
        /// Copies the contents of the given surface to the upper left corner of this surface.
        /// </summary>
        /// <param name="source">The surface to copy pixels from.</param>
        /// <remarks>
        /// The source surface does not need to have the same dimensions as this surface. Clipping
        /// will be handled automatically. No resizing will be done.
        /// </remarks>
        public void CopySurface(Surface source)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("Surface");
            }

            if (this._stride == source._stride &&
                (this._width * ColorBgra.Size) == this._stride &&
                this._width == source._width &&
                this._height == source._height)
            {
                unsafe
                {
                    Memory.Copy(this._scan0.VoidStar,
                                source._scan0.VoidStar,
                                ((ulong)(_height - 1) * (ulong)_stride) + ((ulong)_width * (ulong)ColorBgra.Size));
                }
            }
            else
            {
                int copyWidth = Math.Min(_width, source._width);
                int copyHeight = Math.Min(_height, source._height);

                unsafe
                {
                    for (int y = 0; y < copyHeight; ++y)
                    {
                        Memory.Copy(UnsafeGetRowAddress(y), source.UnsafeGetRowAddress(y), (ulong)copyWidth * (ulong)ColorBgra.Size);
                    }
                }
            }
        }

        /// <summary>
        /// Copies the contents of the given surface to a location within this surface.
        /// </summary>
        /// <param name="source">The surface to copy pixels from.</param>
        /// <param name="dstOffset">
        /// The offset within this surface to start copying pixels to. This will map to (0,0) in the source.
        /// </param>
        /// <remarks>
        /// The source surface does not need to have the same dimensions as this surface. Clipping
        /// will be handled automatically. No resizing will be done.
        /// </remarks>
        public void CopySurface(Surface source, Point dstOffset)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("Surface");
            }

            Rectangle dstRect = new Rectangle(dstOffset, source.Size);
            dstRect.Intersect(Bounds);

            if (dstRect.Width == 0 || dstRect.Height == 0)
            {
                return;
            }

            Point sourceOffset = new Point(dstRect.Location.X - dstOffset.X, dstRect.Location.Y - dstOffset.Y);
            Rectangle sourceRect = new Rectangle(sourceOffset, dstRect.Size);
            Surface sourceWindow = source.CreateWindow(sourceRect);
            Surface dstWindow = this.CreateWindow(dstRect);
            dstWindow.CopySurface(sourceWindow);

            dstWindow.Dispose();
            sourceWindow.Dispose();
        }

        /// <summary>
        /// Copies the contents of the given surface to the upper left of this surface.
        /// </summary>
        /// <param name="source">The surface to copy pixels from.</param>
        /// <param name="sourceRoi">
        /// The region of the source to copy from. The upper left of this rectangle
        /// will be mapped to (0,0) on this surface.
        /// The source surface does not need to have the same dimensions as this surface. Clipping
        /// will be handled automatically. No resizing will be done.
        /// </param>
        public void CopySurface(Surface source, Rectangle sourceRoi)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("Surface");
            }

            sourceRoi.Intersect(source.Bounds);
            int copiedWidth = Math.Min(this._width, sourceRoi.Width);
            int copiedHeight = Math.Min(this.Height, sourceRoi.Height);

            if (copiedWidth == 0 || copiedHeight == 0)
            {
                return;
            }

            using (Surface src = source.CreateWindow(sourceRoi))
            {
                CopySurface(src);
            }
        }

        /// <summary>
        /// Copies a rectangular region of the given surface to a specific location on this surface.
        /// </summary>
        /// <param name="source">The surface to copy pixels from.</param>
        /// <param name="dstOffset">The location on this surface to start copying pixels to.</param>
        /// <param name="sourceRoi">The region of the source surface to copy pixels from.</param>
        /// <remarks>
        /// sourceRoi.Location will be mapped to dstOffset.Location.
        /// The source surface does not need to have the same dimensions as this surface. Clipping
        /// will be handled automatically. No resizing will be done.
        /// </remarks>
        public void CopySurface(Surface source, Point dstOffset, Rectangle sourceRoi)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("Surface");
            }

            Rectangle dstRoi = new Rectangle(dstOffset, sourceRoi.Size);
            dstRoi.Intersect(Bounds);

            if (dstRoi.Height == 0 || dstRoi.Width == 0)
            {
                return;
            }

            sourceRoi.X += dstRoi.X - dstOffset.X;
            sourceRoi.Y += dstRoi.Y - dstOffset.Y;
            sourceRoi.Width = dstRoi.Width;
            sourceRoi.Height = dstRoi.Height;

            using (Surface src = source.CreateWindow(sourceRoi))
            {
                CopySurface(src, dstOffset);
            }
        }

        /// <summary>
        /// Copies a region of the given surface to this surface.
        /// </summary>
        /// <param name="source">The surface to copy pixels from.</param>
        /// <param name="region">The region to clip copying to.</param>
        /// <remarks>
        /// The upper left corner of the source surface will be mapped to the upper left of this
        /// surface, and only those pixels that are defined by the region will be copied.
        /// The source surface does not need to have the same dimensions as this surface. Clipping
        /// will be handled automatically. No resizing will be done.
        /// </remarks>
        public void CopySurface(Surface source, GeometryRegion region)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("Surface");
            }

            Rectangle[] scans = region.GetRegionScansReadOnlyInt();
            for (int i = 0; i < scans.Length; ++i)
            {
                Rectangle rect = scans[i];

                rect.Intersect(this.Bounds);
                rect.Intersect(source.Bounds);

                if (rect.Width == 0 || rect.Height == 0)
                {
                    continue;
                }

                unsafe
                {
                    for (int y = rect.Top; y < rect.Bottom; ++y)
                    {
                        ColorBgra* dst = this.UnsafeGetPointAddress(rect.Left, y);
                        ColorBgra* src = source.UnsafeGetPointAddress(rect.Left, y);
                        Memory.Copy(dst, src, (ulong)rect.Width * (ulong)ColorBgra.Size);
                    }
                }
            }
        }

        /// <summary>
        /// Copies a region of the given surface to this surface.
        /// </summary>
        /// <param name="source">The surface to copy pixels from.</param>
        /// <param name="region">The region to clip copying to.</param>
        /// <remarks>
        /// The upper left corner of the source surface will be mapped to the upper left of this
        /// surface, and only those pixels that are defined by the region will be copied.
        /// The source surface does not need to have the same dimensions as this surface. Clipping
        /// will be handled automatically. No resizing will be done.
        /// </remarks>
        public void CopySurface(Surface source, Rectangle[] region, int startIndex, int length)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("Surface");
            }

            for (int i = startIndex; i < startIndex + length; ++i)
            {
                Rectangle rect = region[i];

                rect.Intersect(this.Bounds);
                rect.Intersect(source.Bounds);

                if (rect.Width == 0 || rect.Height == 0)
                {
                    continue;
                }

                unsafe
                {
                    for (int y = rect.Top; y < rect.Bottom; ++y)
                    {
                        ColorBgra* dst = this.UnsafeGetPointAddress(rect.Left, y);
                        ColorBgra* src = source.UnsafeGetPointAddress(rect.Left, y);
                        Memory.Copy(dst, src, (ulong)rect.Width * (ulong)ColorBgra.Size);
                    }
                }
            }
        }

        public void CopySurface(Surface source, Rectangle[] region)
        {
            CopySurface(source, region, 0, region.Length);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        /// <summary>
        /// Creates a new surface with the same dimensions and pixel values as this one.
        /// </summary>
        /// <returns>A new surface that is a clone of the current one.</returns>
        public Surface Clone()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("Surface");
            }

            Surface ret = new Surface(this.Size);
            ret.CopySurface(this);
            return ret;
        }

        #endregion

        #region Clear surface

        /// <summary>
        /// Clears the surface to all-white (BGRA = [255,255,255,255]).
        /// </summary>
        public void Clear()
        {
            Clear(ColorBgra.FromBgra(255, 255, 255, 255));
        }

        /// <summary>
        /// Clears the surface to the given color value.
        /// </summary>
        /// <param name="color">The color value to fill the surface with.</param>
        public void Clear(ColorBgra color)
        {
            new UnaryPixelOperations.Constant(color).Apply(this, this.Bounds);
        }

        /// <summary>
        /// Clears the given rectangular region within the surface to the given color value.
        /// </summary>
        /// <param name="color">The color value to fill the rectangular region with.</param>
        /// <param name="rect">The rectangular region to fill.</param>
        public void Clear(Rectangle rect, ColorBgra color)
        {
            Rectangle rect2 = Rectangle.Intersect(this.Bounds, rect);

            if (rect2 != rect)
            {
                throw new ArgumentOutOfRangeException("rectangle is out of bounds");
            }

            new UnaryPixelOperations.Constant(color).Apply(this, rect);
        }

        public void Clear(GeometryRegion region, ColorBgra color)
        {
            foreach (Rectangle rect in region.GetRegionScansReadOnlyInt())
            {
                Clear(rect, color);
            }
        }

        public void ClearWithCheckboardPattern()
        {
            unsafe
            {
                for (int y = 0; y < this._height; ++y)
                {
                    ColorBgra* dstPtr = UnsafeGetRowAddress(y);

                    for (int x = 0; x < this._width; ++x)
                    {
                        byte v = (byte)((((x ^ y) & 8) * 8) + 191);
                        *dstPtr = ColorBgra.FromBgra(v, v, v, 255);
                        ++dstPtr;
                    }
                }
            }
        }

        #endregion

        private double CubeClamped(double x)
        {
            if (x >= 0)
            {
                return x * x * x;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Implements R() as defined at http://astronomy.swin.edu.au/%7Epbourke/colour/bicubic/
        /// </summary>
        private double R(double x)
        {
            return (CubeClamped(x + 2) - (4 * CubeClamped(x + 1)) + (6 * CubeClamped(x)) - (4 * CubeClamped(x - 1))) / 6;
        }

        #region ResamplingAlgorithms

        /// <summary>
        /// Fits the source surface to this surface using super sampling. If the source surface is less wide
        /// or less tall than this surface (i.e. magnification), bicubic resampling is used instead. If either
        /// the source or destination has a dimension that is only 1 pixel, nearest neighbor is used.
        /// </summary>
        /// <param name="source">The Surface to read pixels from.</param>
        /// <remarks>This method was implemented with correctness, not performance, in mind.</remarks>
        public void SuperSamplingFitSurface(Surface source)
        {
            SuperSamplingFitSurface(source, this.Bounds);
        }

        /// <summary>
        /// Fits the source surface to this surface using super sampling. If the source surface is less wide
        /// or less tall than this surface (i.e. magnification), bicubic resampling is used instead. If either
        /// the source or destination has a dimension that is only 1 pixel, nearest neighbor is used.
        /// </summary>
        /// <param name="source">The surface to read pixels from.</param>
        /// <param name="dstRoi">The rectangle to clip rendering to.</param>
        /// <remarks>This method was implemented with correctness, not performance, in mind.</remarks>
        public void SuperSamplingFitSurface(Surface source, Rectangle dstRoi)
        {
            if (source.Width == Width && source.Height == Height)
            {
                CopySurface(source);
            }
            else if (source.Width <= Width || source.Height <= Height)
            {
                if (source._width < 2 || source._height < 2 || this._width < 2 || this._height < 2)
                {
                    this.NearestNeighborFitSurface(source, dstRoi);
                }
                else
                {
                    this.BicubicFitSurface(source, dstRoi);
                }
            }
            else
                unsafe
                {
                    Rectangle dstRoi2 = Rectangle.Intersect(dstRoi, this.Bounds);

                    for (int dstY = dstRoi2.Top; dstY < dstRoi2.Bottom; ++dstY)
                    {
                        double srcTop = (double)(dstY * source._height) / (double)_height;
                        double srcTopFloor = Math.Floor(srcTop);
                        double srcTopWeight = 1 - (srcTop - srcTopFloor);
                        int srcTopInt = (int)srcTopFloor;

                        double srcBottom = (double)((dstY + 1) * source._height) / (double)_height;
                        double srcBottomFloor = Math.Floor(srcBottom - 0.00001);
                        double srcBottomWeight = srcBottom - srcBottomFloor;
                        var srcBottomInt = (int)srcBottomFloor;

                        ColorBgra* dstPtr = this.UnsafeGetPointAddress(dstRoi2.Left, dstY);

                        for (int dstX = dstRoi2.Left; dstX < dstRoi2.Right; ++dstX)
                        {
                            double srcLeft = (double)(dstX * source._width) / (double)_width;
                            double srcLeftFloor = Math.Floor(srcLeft);
                            double srcLeftWeight = 1 - (srcLeft - srcLeftFloor);
                            int srcLeftInt = (int)srcLeftFloor;

                            double srcRight = (double)((dstX + 1) * source._width) / (double)_width;
                            double srcRightFloor = Math.Floor(srcRight - 0.00001);
                            double srcRightWeight = srcRight - srcRightFloor;
                            int srcRightInt = (int)srcRightFloor;

                            double blueSum = 0;
                            double greenSum = 0;
                            double redSum = 0;
                            double alphaSum = 0;

                            // left fractional edge
                            ColorBgra* srcLeftPtr = source.UnsafeGetPointAddress(srcLeftInt, srcTopInt + 1);

                            for (int srcY = srcTopInt + 1; srcY < srcBottomInt; ++srcY)
                            {
                                double a = srcLeftPtr->A;
                                blueSum += srcLeftPtr->B * srcLeftWeight * a;
                                greenSum += srcLeftPtr->G * srcLeftWeight * a;
                                redSum += srcLeftPtr->R * srcLeftWeight * a;
                                alphaSum += srcLeftPtr->A * srcLeftWeight;
                                srcLeftPtr = (ColorBgra*)((byte*)srcLeftPtr + source._stride);
                            }

                            // right fractional edge
                            ColorBgra* srcRightPtr = source.UnsafeGetPointAddress(srcRightInt, srcTopInt + 1);
                            for (int srcY = srcTopInt + 1; srcY < srcBottomInt; ++srcY)
                            {
                                double a = srcRightPtr->A;
                                blueSum += srcRightPtr->B * srcRightWeight * a;
                                greenSum += srcRightPtr->G * srcRightWeight * a;
                                redSum += srcRightPtr->R * srcRightWeight * a;
                                alphaSum += srcRightPtr->A * srcRightWeight;
                                srcRightPtr = (ColorBgra*)((byte*)srcRightPtr + source._stride);
                            }

                            // top fractional edge
                            ColorBgra* srcTopPtr = source.UnsafeGetPointAddress(srcLeftInt + 1, srcTopInt);
                            for (int srcX = srcLeftInt + 1; srcX < srcRightInt; ++srcX)
                            {
                                double a = srcTopPtr->A;
                                blueSum += srcTopPtr->B * srcTopWeight * a;
                                greenSum += srcTopPtr->G * srcTopWeight * a;
                                redSum += srcTopPtr->R * srcTopWeight * a;
                                alphaSum += srcTopPtr->A * srcTopWeight;
                                ++srcTopPtr;
                            }

                            // bottom fractional edge
                            ColorBgra* srcBottomPtr = source.UnsafeGetPointAddress(srcLeftInt + 1, srcBottomInt);
                            for (int srcX = srcLeftInt + 1; srcX < srcRightInt; ++srcX)
                            {
                                double a = srcBottomPtr->A;
                                blueSum += srcBottomPtr->B * srcBottomWeight * a;
                                greenSum += srcBottomPtr->G * srcBottomWeight * a;
                                redSum += srcBottomPtr->R * srcBottomWeight * a;
                                alphaSum += srcBottomPtr->A * srcBottomWeight;
                                ++srcBottomPtr;
                            }

                            // center area
                            for (int srcY = srcTopInt + 1; srcY < srcBottomInt; ++srcY)
                            {
                                ColorBgra* srcPtr = source.UnsafeGetPointAddress(srcLeftInt + 1, srcY);

                                for (int srcX = srcLeftInt + 1; srcX < srcRightInt; ++srcX)
                                {
                                    double a = srcPtr->A;
                                    blueSum += (double)srcPtr->B * a;
                                    greenSum += (double)srcPtr->G * a;
                                    redSum += (double)srcPtr->R * a;
                                    alphaSum += (double)srcPtr->A;
                                    ++srcPtr;
                                }
                            }

                            // four corner pixels
                            ColorBgra srcTL = source.GetPoint(srcLeftInt, srcTopInt);
                            double srcTLA = srcTL.A;
                            blueSum += srcTL.B * (srcTopWeight * srcLeftWeight) * srcTLA;
                            greenSum += srcTL.G * (srcTopWeight * srcLeftWeight) * srcTLA;
                            redSum += srcTL.R * (srcTopWeight * srcLeftWeight) * srcTLA;
                            alphaSum += srcTL.A * (srcTopWeight * srcLeftWeight);

                            ColorBgra srcTR = source.GetPoint(srcRightInt, srcTopInt);
                            double srcTRA = srcTR.A;
                            blueSum += srcTR.B * (srcTopWeight * srcRightWeight) * srcTRA;
                            greenSum += srcTR.G * (srcTopWeight * srcRightWeight) * srcTRA;
                            redSum += srcTR.R * (srcTopWeight * srcRightWeight) * srcTRA;
                            alphaSum += srcTR.A * (srcTopWeight * srcRightWeight);

                            ColorBgra srcBL = source.GetPoint(srcLeftInt, srcBottomInt);
                            double srcBLA = srcBL.A;
                            blueSum += srcBL.B * (srcBottomWeight * srcLeftWeight) * srcBLA;
                            greenSum += srcBL.G * (srcBottomWeight * srcLeftWeight) * srcBLA;
                            redSum += srcBL.R * (srcBottomWeight * srcLeftWeight) * srcBLA;
                            alphaSum += srcBL.A * (srcBottomWeight * srcLeftWeight);

                            ColorBgra srcBR = source.GetPoint(srcRightInt, srcBottomInt);
                            double srcBRA = srcBR.A;
                            blueSum += srcBR.B * (srcBottomWeight * srcRightWeight) * srcBRA;
                            greenSum += srcBR.G * (srcBottomWeight * srcRightWeight) * srcBRA;
                            redSum += srcBR.R * (srcBottomWeight * srcRightWeight) * srcBRA;
                            alphaSum += srcBR.A * (srcBottomWeight * srcRightWeight);

                            double area = (srcRight - srcLeft) * (srcBottom - srcTop);

                            double alpha = alphaSum / area;
                            double blue;
                            double green;
                            double red;

                            if (alpha == 0)
                            {
                                blue = 0;
                                green = 0;
                                red = 0;
                            }
                            else
                            {
                                blue = blueSum / alphaSum;
                                green = greenSum / alphaSum;
                                red = redSum / alphaSum;
                            }

                            // add 0.5 so that rounding goes in the direction we want it to
                            blue += 0.5;
                            green += 0.5;
                            red += 0.5;
                            alpha += 0.5;

                            dstPtr->Bgra = (uint)blue + ((uint)green << 8) + ((uint)red << 16) + ((uint)alpha << 24);
                            ++dstPtr;
                        }
                    }
                }
        }

        /// <summary>
        /// Fits the source surface to this surface using nearest neighbor resampling.
        /// </summary>
        /// <param name="source">The surface to read pixels from.</param>
        public void NearestNeighborFitSurface(Surface source)
        {
            NearestNeighborFitSurface(source, this.Bounds);
        }

        /// <summary>
        /// Fits the source surface to this surface using nearest neighbor resampling.
        /// </summary>
        /// <param name="source">The surface to read pixels from.</param>
        /// <param name="dstRoi">The rectangle to clip rendering to.</param>
        public void NearestNeighborFitSurface(Surface source, Rectangle dstRoi)
        {
            Rectangle roi = Rectangle.Intersect(dstRoi, this.Bounds);

            unsafe
            {
                for (int dstY = roi.Top; dstY < roi.Bottom; ++dstY)
                {
                    int srcY = (dstY * source._height) / _height;
                    ColorBgra* srcRow = source.UnsafeGetRowAddress(srcY);
                    ColorBgra* dstPtr = this.UnsafeGetPointAddress(roi.Left, dstY);

                    for (int dstX = roi.Left; dstX < roi.Right; ++dstX)
                    {
                        int srcX = (dstX * source._width) / _width;
                        *dstPtr = *(srcRow + srcX);
                        ++dstPtr;
                    }
                }
            }
        }

        /// <summary>
        /// Fits the source surface to this surface using bicubic interpolation.
        /// </summary>
        /// <param name="source">The Surface to read pixels from.</param>
        /// <remarks>
        /// This method was implemented with correctness, not performance, in mind. 
        /// Based on: "Bicubic Interpolation for Image Scaling" by Paul Bourke,
        ///           http://astronomy.swin.edu.au/%7Epbourke/colour/bicubic/
        /// </remarks>
        public void BicubicFitSurface(Surface source)
        {
            BicubicFitSurface(source, this.Bounds);
        }

        /// <summary>
        /// Fits the source surface to this surface using bicubic interpolation.
        /// </summary>
        /// <param name="source">The Surface to read pixels from.</param>
        /// <param name="dstRoi">The rectangle to clip rendering to.</param>
        /// <remarks>
        /// This method was implemented with correctness, not performance, in mind. 
        /// Based on: "Bicubic Interpolation for Image Scaling" by Paul Bourke,
        ///           http://astronomy.swin.edu.au/%7Epbourke/colour/bicubic/
        /// </remarks>
        public void BicubicFitSurface(Surface source, Rectangle dstRoi)
        {
            var leftF = (1 * (float)(_width - 1)) / (source._width - 1);
            var topF = (1 * (_height - 1)) / (float)(source._height - 1);
            var rightF = (float)((source._width - 3) * (_width - 1)) / (source._width - 1);
            var bottomF = (float)((source.Height - 3) * (_height - 1)) / (source._height - 1);

            var left = (int)Math.Ceiling(leftF);
            var top = (int)Math.Ceiling(topF);
            var right = (int)Math.Floor(rightF);
            var bottom = (int)Math.Floor(bottomF);

            var rois = new[]
            {
                Rectangle.FromLTRB(left, top, right, bottom),
                new Rectangle(0, 0, _width, top),
                new Rectangle(0, top, left, _height - top),
                new Rectangle(right, top, _width - right, _height - top),
                new Rectangle(left, bottom, right - left, _height - bottom)
            };

            for (int i = 0; i < rois.Length; ++i)
            {
                rois[i].Intersect(dstRoi);

                if (rois[i].Width > 0 && rois[i].Height > 0)
                {
                    if (i == 0)
                    {
                        BicubicFitSurfaceUnchecked(source, rois[i]);
                    }
                    else
                    {
                        BicubicFitSurfaceChecked(source, rois[i]);
                    }
                }
            }
        }

        /// <summary>
        /// Implements bicubic filtering with bounds checking at every pixel.
        /// </summary>
        private void BicubicFitSurfaceChecked(Surface source, Rectangle dstRoi)
        {
            if (this._width < 2 || this._height < 2 || source._width < 2 || source._height < 2)
            {
                SuperSamplingFitSurface(source, dstRoi);
            }
            else
            {
                unsafe
                {
                    Rectangle roi = Rectangle.Intersect(dstRoi, this.Bounds);
                    Rectangle roiIn = Rectangle.Intersect(dstRoi, new Rectangle(1, 1, _width - 1, _height - 1));

                    IntPtr rColCacheIP = Memory.Allocate(4 * (ulong)roi.Width * (ulong)sizeof(double));
                    double* rColCache = (double*)rColCacheIP.ToPointer();

                    // Precompute and then cache the value of R() for each column
                    for (int dstX = roi.Left; dstX < roi.Right; ++dstX)
                    {
                        double srcColumn = (double)(dstX * (source._width - 1)) / (double)(_width - 1);
                        double srcColumnFloor = Math.Floor(srcColumn);
                        double srcColumnFrac = srcColumn - srcColumnFloor;

                        for (int m = -1; m <= 2; ++m)
                        {
                            int index = (m + 1) + ((dstX - roi.Left) * 4);
                            double x = m - srcColumnFrac;
                            rColCache[index] = R(x);
                        }
                    }

                    // Set this up so we can cache the R()'s for every row
                    double* rRowCache = stackalloc double[4];

                    for (int dstY = roi.Top; dstY < roi.Bottom; ++dstY)
                    {
                        double srcRow = (double)(dstY * (source._height - 1)) / (double)(_height - 1);
                        double srcRowFloor = (double)Math.Floor(srcRow);
                        double srcRowFrac = srcRow - srcRowFloor;
                        int srcRowInt = (int)srcRow;
                        ColorBgra* dstPtr = this.UnsafeGetPointAddress(roi.Left, dstY);

                        // Compute the R() values for this row
                        for (int n = -1; n <= 2; ++n)
                        {
                            double x = srcRowFrac - n;
                            rRowCache[n + 1] = R(x);
                        }

                        // See Perf Note below
                        //int nFirst = Math.Max(-srcRowInt, -1);
                        //int nLast = Math.Min(source.height - srcRowInt - 1, 2);

                        for (int dstX = roi.Left; dstX < roi.Right; dstX++)
                        {
                            double srcColumn = (double)(dstX * (source._width - 1)) / (double)(_width - 1);
                            int srcColumnInt = (int)srcColumn;

                            double blueSum = 0;
                            double greenSum = 0;
                            double redSum = 0;
                            double alphaSum = 0;
                            double totalWeight = 0;

                            // See Perf Note below
                            //int mFirst = Math.Max(-srcColumnInt, -1);
                            //int mLast = Math.Min(source.width - srcColumnInt - 1, 2);

                            ColorBgra* srcPtr = source.UnsafeGetPointAddress(srcColumnInt - 1, srcRowInt - 1);

                            for (int n = -1; n <= 2; ++n)
                            {
                                int srcY = srcRowInt + n;

                                for (int m = -1; m <= 2; ++m)
                                {
                                    // Perf Note: It actually benchmarks faster on my system to do
                                    // a bounds check for every (m,n) than it is to limit the loop
                                    // to nFirst-Last and mFirst-mLast.
                                    // I'm leaving the code above, albeit commented out, so that
                                    // benchmarking between these two can still be performed.
                                    if (source.IsVisible(srcColumnInt + m, srcY))
                                    {
                                        double w0 = rColCache[(m + 1) + (4 * (dstX - roi.Left))];
                                        double w1 = rRowCache[n + 1];
                                        double w = w0 * w1;

                                        blueSum += srcPtr->B * w * srcPtr->A;
                                        greenSum += srcPtr->G * w * srcPtr->A;
                                        redSum += srcPtr->R * w * srcPtr->A;
                                        alphaSum += srcPtr->A * w;

                                        totalWeight += w;
                                    }

                                    ++srcPtr;
                                }

                                srcPtr = (ColorBgra*)((byte*)(srcPtr - 4) + source._stride);
                            }

                            double alpha = alphaSum / totalWeight;
                            double blue;
                            double green;
                            double red;

                            if (alpha == 0)
                            {
                                blue = 0;
                                green = 0;
                                red = 0;
                            }
                            else
                            {
                                blue = blueSum / alphaSum;
                                green = greenSum / alphaSum;
                                red = redSum / alphaSum;

                                // add 0.5 to ensure truncation to uint results in rounding
                                alpha += 0.5;
                                blue += 0.5;
                                green += 0.5;
                                red += 0.5;
                            }

                            dstPtr->Bgra = (uint)blue + ((uint)green << 8) + ((uint)red << 16) + ((uint)alpha << 24);
                            ++dstPtr;
                        } // for (dstX...
                    } // for (dstY...

                    Memory.Free(rColCacheIP);
                } // unsafe
            }
        }

        /// <summary>
        /// Implements bicubic filtering with NO bounds checking at any pixel.
        /// </summary>
        public void BicubicFitSurfaceUnchecked(Surface source, Rectangle dstRoi)
        {
            if (this._width < 2 || this._height < 2 || source._width < 2 || source._height < 2)
            {
                SuperSamplingFitSurface(source, dstRoi);
            }
            else
            {
                unsafe
                {
                    Rectangle roi = Rectangle.Intersect(dstRoi, this.Bounds);
                    Rectangle roiIn = Rectangle.Intersect(dstRoi, new Rectangle(1, 1, _width - 1, _height - 1));

                    IntPtr rColCacheIP = Memory.Allocate(4 * (ulong)roi.Width * (ulong)sizeof(double));
                    double* rColCache = (double*)rColCacheIP.ToPointer();

                    // Precompute and then cache the value of R() for each column
                    for (int dstX = roi.Left; dstX < roi.Right; ++dstX)
                    {
                        double srcColumn = (double)(dstX * (source._width - 1)) / (double)(_width - 1);
                        double srcColumnFloor = Math.Floor(srcColumn);
                        double srcColumnFrac = srcColumn - srcColumnFloor;
                        int srcColumnInt = (int)srcColumn;

                        for (int m = -1; m <= 2; ++m)
                        {
                            int index = (m + 1) + ((dstX - roi.Left) * 4);
                            double x = m - srcColumnFrac;
                            rColCache[index] = R(x);
                        }
                    }

                    // Set this up so we can cache the R()'s for every row
                    double* rRowCache = stackalloc double[4];

                    for (int dstY = roi.Top; dstY < roi.Bottom; ++dstY)
                    {
                        double srcRow = (double)(dstY * (source._height - 1)) / (double)(_height - 1);
                        double srcRowFloor = Math.Floor(srcRow);
                        double srcRowFrac = srcRow - srcRowFloor;
                        int srcRowInt = (int)srcRow;
                        ColorBgra* dstPtr = this.UnsafeGetPointAddress(roi.Left, dstY);

                        // Compute the R() values for this row
                        for (int n = -1; n <= 2; ++n)
                        {
                            double x = srcRowFrac - n;
                            rRowCache[n + 1] = R(x);
                        }

                        rColCache = (double*)rColCacheIP.ToPointer();
                        ColorBgra* srcRowPtr = source.UnsafeGetRowAddress(srcRowInt - 1);

                        for (int dstX = roi.Left; dstX < roi.Right; dstX++)
                        {
                            double srcColumn = (double)(dstX * (source._width - 1)) / (double)(_width - 1);
                            double srcColumnFloor = Math.Floor(srcColumn);
                            double srcColumnFrac = srcColumn - srcColumnFloor;
                            int srcColumnInt = (int)srcColumn;

                            double blueSum = 0;
                            double greenSum = 0;
                            double redSum = 0;
                            double alphaSum = 0;
                            double totalWeight = 0;

                            ColorBgra* srcPtr = srcRowPtr + srcColumnInt - 1;
                            for (int n = 0; n <= 3; ++n)
                            {
                                double w0 = rColCache[0] * rRowCache[n];
                                double w1 = rColCache[1] * rRowCache[n];
                                double w2 = rColCache[2] * rRowCache[n];
                                double w3 = rColCache[3] * rRowCache[n];

                                double a0 = srcPtr[0].A;
                                double a1 = srcPtr[1].A;
                                double a2 = srcPtr[2].A;
                                double a3 = srcPtr[3].A;

                                alphaSum += (a0 * w0) + (a1 * w1) + (a2 * w2) + (a3 * w3);
                                totalWeight += w0 + w1 + w2 + w3;

                                blueSum += (a0 * srcPtr[0].B * w0) + (a1 * srcPtr[1].B * w1) + (a2 * srcPtr[2].B * w2) +
                                           (a3 * srcPtr[3].B * w3);
                                greenSum += (a0 * srcPtr[0].G * w0) + (a1 * srcPtr[1].G * w1) + (a2 * srcPtr[2].G * w2) +
                                            (a3 * srcPtr[3].G * w3);
                                redSum += (a0 * srcPtr[0].R * w0) + (a1 * srcPtr[1].R * w1) + (a2 * srcPtr[2].R * w2) +
                                          (a3 * srcPtr[3].R * w3);

                                srcPtr = (ColorBgra*)((byte*)srcPtr + source._stride);
                            }

                            double alpha = alphaSum / totalWeight;

                            double blue;
                            double green;
                            double red;

                            if (alpha == 0)
                            {
                                blue = 0;
                                green = 0;
                                red = 0;
                            }
                            else
                            {
                                blue = blueSum / alphaSum;
                                green = greenSum / alphaSum;
                                red = redSum / alphaSum;

                                // add 0.5 to ensure truncation to uint results in rounding
                                alpha += 0.5;
                                blue += 0.5;
                                green += 0.5;
                                red += 0.5;
                            }

                            dstPtr->Bgra = (uint)blue + ((uint)green << 8) + ((uint)red << 16) + ((uint)alpha << 24);
                            ++dstPtr;
                            rColCache += 4;
                        } // for (dstX...
                    } // for (dstY...

                    Memory.Free(rColCacheIP);
                } // unsafe
            }
        }

        /// <summary>
        /// Fits the source surface to this surface using bilinear interpolation.
        /// </summary>
        /// <param name="source">The surface to read pixels from.</param>
        /// <remarks>This method was implemented with correctness, not performance, in mind.</remarks>
        public void BilinearFitSurface(Surface source)
        {
            BilinearFitSurface(source, this.Bounds);
        }

        /// <summary>
        /// Fits the source surface to this surface using bilinear interpolation.
        /// </summary>
        /// <param name="source">The surface to read pixels from.</param>
        /// <param name="dstRoi">The rectangle to clip rendering to.</param>
        /// <remarks>This method was implemented with correctness, not performance, in mind.</remarks>
        public void BilinearFitSurface(Surface source, Rectangle dstRoi)
        {
            if (dstRoi.Width < 2 || dstRoi.Height < 2 || this._width < 2 || this._height < 2)
            {
                SuperSamplingFitSurface(source, dstRoi);
            }
            else
            {
                unsafe
                {
                    Rectangle roi = Rectangle.Intersect(dstRoi, this.Bounds);

                    for (int dstY = roi.Top; dstY < roi.Bottom; ++dstY)
                    {
                        ColorBgra* dstRowPtr = this.UnsafeGetRowAddress(dstY);
                        float srcRow = (float)(dstY * (source._height - 1)) / (float)(_height - 1);

                        for (int dstX = roi.Left; dstX < roi.Right; dstX++)
                        {
                            float srcColumn = (float)(dstX * (source._width - 1)) / (float)(_width - 1);
                            *dstRowPtr = source.GetBilinearSample(srcColumn, srcRow);
                            ++dstRowPtr;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Fits the source surface to this surface using the given algorithm.
        /// </summary>
        /// <param name="algorithm">The surface to copy pixels from.</param>
        /// <param name="source">The algorithm to use.</param>
        public void FitSurface(ResamplingAlgorithm algorithm, Surface source)
        {
            FitSurface(algorithm, source, this.Bounds);
        }

        /// <summary>
        /// Fits the source surface to this surface using the given algorithm.
        /// </summary>
        /// <param name="algorithm">The surface to copy pixels from.</param>
        /// <param name="dstRoi">The rectangle to clip rendering to.</param>
        /// <param name="source">The algorithm to use.</param>
        public void FitSurface(ResamplingAlgorithm algorithm, Surface source, Rectangle dstRoi)
        {
            switch (algorithm)
            {
                case ResamplingAlgorithm.Bicubic:
                    BicubicFitSurface(source, dstRoi);
                    break;

                case ResamplingAlgorithm.Bilinear:
                    BilinearFitSurface(source, dstRoi);
                    break;

                case ResamplingAlgorithm.NearestNeighbor:
                    NearestNeighborFitSurface(source, dstRoi);
                    break;

                case ResamplingAlgorithm.SuperSampling:
                    SuperSamplingFitSurface(source, dstRoi);
                    break;

                default:
                    throw new InvalidEnumArgumentException("algorithm");
            }
        }

        #endregion

        #region IDispose

        /// <summary>
        /// Releases all resources held by this Surface object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;

                if (disposing)
                {
                    _scan0.Dispose();
                    _scan0 = null;
                }
            }
        }

        #endregion

    }
}