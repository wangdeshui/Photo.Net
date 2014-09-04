using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Serialization;

namespace Photo.Net.Core.Geometry
{
    /// <summary>
    /// Designed as a proxy to the GDI+ Region class, while allowing for a
    /// replacement that won't break code. The main reason for having this
    /// right now is to work around some bugs in System.Drawing.Region,
    /// especially the memory leak in GetRegionScans().
    /// </summary>
    [Serializable]
    public sealed class GeometryRegion
        : ISerializable,
          IDisposable
    {
        private readonly object _lockObject = new object();
        private Region _gdiRegion;
        private bool _changed = true;
        private int _cachedArea = -1;
        private Rectangle _cachedBounds = Rectangle.Empty;
        private RectangleF[] _cachedRectsF;
        private readonly Rectangle[] _cachedRects;

        public object SyncRoot
        {
            get
            {
                return _lockObject;
            }
        }

        public int GetArea()
        {
            lock (SyncRoot)
            {
                int theCachedArea = _cachedArea;

                if (theCachedArea == -1)
                {
                    int ourCachedArea = 0;

                    foreach (Rectangle rect in GetRegionScansReadOnlyInt())
                    {
                        try
                        {
                            ourCachedArea += rect.Width * rect.Height;
                        }

                        catch (System.OverflowException)
                        {
                            ourCachedArea = int.MaxValue;
                            break;
                        }
                    }

                    _cachedArea = ourCachedArea;
                    return ourCachedArea;
                }
                else
                {
                    return theCachedArea;
                }
            }
        }

        private bool IsChanged()
        {
            return this._changed;
        }

        private void Changed()
        {
            lock (SyncRoot)
            {
                this._changed = true;
                this._cachedArea = -1;
                this._cachedBounds = Rectangle.Empty;
            }
        }

        private void ResetChanged()
        {
            lock (SyncRoot)
            {
                this._changed = false;
            }
        }

        public GeometryRegion()
        {
            this._gdiRegion = new Region();
        }

        public GeometryRegion(GraphicsPath path)
        {
            this._gdiRegion = new Region(path);
        }

        public GeometryRegion(GeometryGraphicsPath pdnPath)
            : this(pdnPath.GetRegionCache())
        {
        }

        public GeometryRegion(Rectangle rect)
        {
            this._gdiRegion = new Region(rect);
        }

        public GeometryRegion(RectangleF rectF)
        {
            this._gdiRegion = new Region(rectF);
        }

        public GeometryRegion(RegionData regionData)
        {
            this._gdiRegion = new Region(regionData);
        }

        public GeometryRegion(Region region, bool takeOwnership)
        {
            if (takeOwnership)
            {
                this._gdiRegion = region;
            }
            else
            {
                this._gdiRegion = region.Clone();
            }
        }

        public GeometryRegion(Region region)
            : this(region, false)
        {
        }

        private GeometryRegion(GeometryRegion GeometryRegion)
        {
            lock (GeometryRegion.SyncRoot)
            {
                this._gdiRegion = GeometryRegion._gdiRegion.Clone();
                this._changed = GeometryRegion._changed;
                this._cachedArea = GeometryRegion._cachedArea;
                this._cachedRectsF = GeometryRegion._cachedRectsF;
                this._cachedRects = GeometryRegion._cachedRects;
            }
        }

        // This constructor is used by WrapRegion. The boolean parameter is just
        // there because we already have a parameterless contructor
        private GeometryRegion(bool sentinel)
        {
        }

        public static GeometryRegion CreateEmpty()
        {
            GeometryRegion region = new GeometryRegion();
            region.MakeEmpty();
            return region;
        }

        public static GeometryRegion WrapRegion(Region region)
        {
            GeometryRegion GeometryRegion = new GeometryRegion(false);
            GeometryRegion._gdiRegion = region;
            return GeometryRegion;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (this._disposed)
            {
                throw new ObjectDisposedException("GeometryRegion");
            }

            RegionData regionData;

            lock (SyncRoot)
            {
                regionData = this._gdiRegion.GetRegionData();
            }

            byte[] data = regionData.Data;
            info.AddValue("data", data);
        }

        public GeometryRegion(SerializationInfo info, StreamingContext context)
        {
            byte[] data = (byte[])info.GetValue("data", typeof(byte[]));

            using (Region region = new Region())
            {
                RegionData regionData = region.GetRegionData();
                regionData.Data = data;
                this._gdiRegion = new Region(regionData);
            }

            this._lockObject = new object();
            this._cachedArea = -1;
            this._cachedBounds = Rectangle.Empty;
            this._changed = true;
            this._cachedRects = null;
            this._cachedRectsF = null;
        }

        public GeometryRegion Clone()
        {
            return new GeometryRegion(this);
        }

        ~GeometryRegion()
        {
            Dispose(false);
        }

        private bool _disposed;

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
                    lock (SyncRoot)
                    {
                        _gdiRegion.Dispose();
                        _gdiRegion = null;
                    }
                }

                _disposed = true;
            }
        }

        public Region GetRegionReadOnly()
        {
            return this._gdiRegion;
        }

        public void Complement(GraphicsPath path)
        {
            lock (SyncRoot)
            {
                Changed();
                _gdiRegion.Complement(path);
            }
        }

        public void Complement(Rectangle rect)
        {
            lock (SyncRoot)
            {
                Changed();
                _gdiRegion.Complement(rect);
            }
        }

        public void Complement(RectangleF rectF)
        {
            lock (SyncRoot)
            {
                Changed();
                _gdiRegion.Complement(rectF);
            }
        }

        public void Complement(Region region)
        {
            lock (SyncRoot)
            {
                Changed();
                _gdiRegion.Complement(region);
            }
        }

        public void Complement(GeometryRegion region2)
        {
            lock (SyncRoot)
            {
                Changed();
                _gdiRegion.Complement(region2._gdiRegion);
            }
        }

        public void Exclude(GraphicsPath path)
        {
            lock (SyncRoot)
            {
                _gdiRegion.Exclude(path);
            }
        }

        public void Exclude(Rectangle rect)
        {
            lock (SyncRoot)
            {
                _gdiRegion.Exclude(rect);
            }
        }

        public void Exclude(RectangleF rectF)
        {
            lock (SyncRoot)
            {
                _gdiRegion.Exclude(rectF);
            }
        }

        public void Exclude(Region region)
        {
            lock (SyncRoot)
            {
                _gdiRegion.Exclude(region);
            }
        }

        public void Exclude(GeometryRegion region2)
        {
            lock (SyncRoot)
            {
                _gdiRegion.Exclude(region2._gdiRegion);
            }
        }

        public RectangleF GetBounds(Graphics g)
        {
            lock (SyncRoot)
            {
                return _gdiRegion.GetBounds(g);
            }
        }

        public Rectangle GetBoundsInt()
        {
            Rectangle bounds;

            lock (SyncRoot)
            {
                bounds = this._cachedBounds;

                if (bounds == Rectangle.Empty)
                {
                    Rectangle[] rects = GetRegionScansReadOnlyInt();

                    if (rects.Length == 0)
                    {
                        return Rectangle.Empty;
                    }

                    bounds = rects[0];

                    for (int i = 1; i < rects.Length; ++i)
                    {
                        bounds = Rectangle.Union(bounds, rects[i]);
                    }

                    this._cachedBounds = bounds;
                }
            }

            return bounds;
        }

        public RegionData GetRegionData()
        {
            lock (SyncRoot)
            {
                return _gdiRegion.GetRegionData();
            }
        }

        public RectangleF[] GetRegionScans()
        {
            return (RectangleF[])GetRegionScansReadOnly().Clone();
        }

        /// <summary>
        /// This is an optimized version of GetRegionScans that returns a reference to the array
        /// that is used to cache the region scans. This mitigates performance when this array
        /// is requested many times on an unmodified GeometryRegion.
        /// Thus, by using this method you are promising to not modify the array that is returned.
        /// </summary>
        /// <returns></returns>
        public RectangleF[] GetRegionScansReadOnly()
        {
            lock (this.SyncRoot)
            {
                if (this._changed)
                {
                    UpdateCachedRegionScans();
                }

                if (this._cachedRectsF == null)
                {
                    this._cachedRectsF = new RectangleF[_cachedRects.Length];

                    for (int i = 0; i < this._cachedRectsF.Length; ++i)
                    {
                        this._cachedRectsF[i] = (RectangleF)this._cachedRects[i];
                    }
                }

                return this._cachedRectsF;
            }
        }

        public Rectangle[] GetRegionScansInt()
        {
            return (Rectangle[])GetRegionScansReadOnlyInt().Clone();
        }

        public Rectangle[] GetRegionScansReadOnlyInt()
        {
            lock (this.SyncRoot)
            {
                if (this._changed)
                {
                    UpdateCachedRegionScans();
                }

                return this._cachedRects;
            }
        }

        private unsafe void UpdateCachedRegionScans()
        {
            // Assumes we are in a lock(SyncRoot){} block
            //            PtnGraphics.GetRegionScans(this._gdiRegion, out _cachedRects, out _cachedArea);
            this._cachedRectsF = null; // only update this when specifically asked for it
        }

        public void Intersect(GraphicsPath path)
        {
            lock (SyncRoot)
            {
                Changed();
                _gdiRegion.Intersect(path);
            }
        }

        public void Intersect(Rectangle rect)
        {
            lock (SyncRoot)
            {
                Changed();
                _gdiRegion.Intersect(rect);
            }
        }

        public void Intersect(RectangleF rectF)
        {
            lock (SyncRoot)
            {
                Changed();
                _gdiRegion.Intersect(rectF);
            }
        }

        public void Intersect(Region region)
        {
            lock (SyncRoot)
            {
                Changed();
                _gdiRegion.Intersect(region);
            }
        }

        public void Intersect(GeometryRegion region2)
        {
            lock (SyncRoot)
            {
                Changed();
                _gdiRegion.Intersect(region2._gdiRegion);
            }
        }

        public bool IsEmpty(Graphics g)
        {
            lock (SyncRoot)
            {
                return _gdiRegion.IsEmpty(g);
            }
        }

        public bool IsEmpty()
        {
            return GetArea() == 0;
        }

        public bool IsInfinite(Graphics g)
        {
            lock (SyncRoot)
            {
                return _gdiRegion.IsInfinite(g);
            }
        }

        public bool IsVisible(Point point)
        {
            lock (SyncRoot)
            {
                return _gdiRegion.IsVisible(point);
            }
        }

        public bool IsVisible(PointF pointF)
        {
            lock (SyncRoot)
            {
                return _gdiRegion.IsVisible(pointF);
            }
        }

        public bool IsVisible(Rectangle rect)
        {
            lock (SyncRoot)
            {
                return _gdiRegion.IsVisible(rect);
            }
        }

        public bool IsVisible(RectangleF rectF)
        {
            lock (SyncRoot)
            {
                return _gdiRegion.IsVisible(rectF);
            }
        }

        public bool IsVisible(Point point, Graphics g)
        {
            lock (SyncRoot)
            {
                return _gdiRegion.IsVisible(point, g);
            }
        }

        public bool IsVisible(PointF pointF, Graphics g)
        {
            lock (SyncRoot)
            {
                return _gdiRegion.IsVisible(pointF, g);
            }
        }

        public bool IsVisible(Rectangle rect, Graphics g)
        {
            lock (SyncRoot)
            {
                return _gdiRegion.IsVisible(rect, g);
            }
        }

        public bool IsVisible(RectangleF rectF, Graphics g)
        {
            lock (SyncRoot)
            {
                return _gdiRegion.IsVisible(rectF, g);
            }
        }

        public bool IsVisible(float x, float y)
        {
            lock (SyncRoot)
            {
                return _gdiRegion.IsVisible(x, y);
            }
        }

        public bool IsVisible(int x, int y, Graphics g)
        {
            lock (SyncRoot)
            {
                return _gdiRegion.IsVisible(x, y, g);
            }
        }

        public bool IsVisible(float x, float y, Graphics g)
        {
            lock (SyncRoot)
            {
                return _gdiRegion.IsVisible(x, y, g);
            }
        }

        public bool IsVisible(int x, int y, int width, int height)
        {
            lock (SyncRoot)
            {
                return _gdiRegion.IsVisible(x, y, width, height);
            }
        }

        public bool IsVisible(float x, float y, float width, float height)
        {
            lock (SyncRoot)
            {
                return _gdiRegion.IsVisible(x, y, width, height);
            }
        }

        public bool IsVisible(int x, int y, int width, int height, Graphics g)
        {
            lock (SyncRoot)
            {
                return _gdiRegion.IsVisible(x, y, width, height, g);
            }
        }

        public bool IsVisible(float x, float y, float width, float height, Graphics g)
        {
            lock (SyncRoot)
            {
                return _gdiRegion.IsVisible(x, y, width, height, g);
            }
        }

        public void MakeEmpty()
        {
            lock (SyncRoot)
            {
                Changed();
                _gdiRegion.MakeEmpty();
            }
        }

        public void MakeInfinite()
        {
            lock (SyncRoot)
            {
                Changed();
                _gdiRegion.MakeInfinite();
            }
        }

        public void Transform(Matrix matrix)
        {
            lock (SyncRoot)
            {
                Changed();
                _gdiRegion.Transform(matrix);
            }
        }

        public void Union(GraphicsPath path)
        {
            lock (SyncRoot)
            {
                Changed();
                _gdiRegion.Union(path);
            }
        }

        public void Union(Rectangle rect)
        {
            lock (SyncRoot)
            {
                Changed();
                _gdiRegion.Union(rect);
            }
        }

        public void Union(RectangleF rectF)
        {
            lock (SyncRoot)
            {
                Changed();
                _gdiRegion.Union(rectF);
            }
        }

        public void Union(RectangleF[] rectsF)
        {
            lock (SyncRoot)
            {
                Changed();

                using (GeometryRegion tempRegion = Utility.RectanglesToRegion(rectsF))
                {
                    this.Union(tempRegion);
                }
            }
        }

        public void Union(Region region)
        {
            lock (SyncRoot)
            {
                Changed();
                _gdiRegion.Union(region);
            }
        }

        public void Union(GeometryRegion region2)
        {
            lock (SyncRoot)
            {
                Changed();
                _gdiRegion.Union(region2._gdiRegion);
            }
        }

        public void Xor(Rectangle rect)
        {
            lock (SyncRoot)
            {
                Changed();
                _gdiRegion.Xor(rect);
            }
        }

        public void Xor(RectangleF rectF)
        {
            lock (SyncRoot)
            {
                Changed();
                _gdiRegion.Xor(rectF);
            }
        }

        public void Xor(Region region)
        {
            lock (SyncRoot)
            {
                Changed();
                _gdiRegion.Xor(region);
            }
        }

        public void Xor(GeometryRegion region2)
        {
            lock (SyncRoot)
            {
                Changed();
                _gdiRegion.Xor(region2._gdiRegion);
            }
        }
    }
}