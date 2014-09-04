using System;
using System.Drawing;
using Photo.Net.Core;
using Photo.Net.Core.Geometry;

namespace Photo.Net.Gdi.Surfaces
{
    /// <summary>
    /// This class handles rendering to a SurfaceBox.
    /// </summary>
    public abstract class SurfaceBoxRenderer
        : IDisposable
    {
        private bool _disposed;
        private readonly SurfaceBoxRenderList _ownerList;
        private bool _visible;

        public const int MinXCoordinate = -131072;
        public const int MaxXCoordinate = +131072;
        public const int MinYCoordinate = -131072;
        public const int MaxYCoordinate = +131072;

        public bool IsDisposed
        {
            get
            {
                return this._disposed;
            }
        }

        public static Rectangle MaxBounds
        {
            get
            {
                return Rectangle.FromLTRB(MinXCoordinate, MinYCoordinate, MaxXCoordinate + 1, MaxYCoordinate + 1);
            }
        }

        protected object SyncRoot
        {
            get
            {
                return OwnerList.SyncRoot;
            }
        }

        protected SurfaceBoxRenderList OwnerList
        {
            get
            {
                return this._ownerList;
            }
        }

        public virtual void OnSourceSizeChanged()
        {
        }

        public virtual void OnDestinationSizeChanged()
        {
        }

        public Size SourceSize
        {
            get
            {
                return this.OwnerList.SourceSize;
            }
        }

        public Size DestinationSize
        {
            get
            {
                return this.OwnerList.DestinationSize;
            }
        }

        protected virtual void OnVisibleChanging()
        {
        }

        protected abstract void OnVisibleChanged();

        public bool Visible
        {
            get
            {
                return this._visible;
            }

            set
            {
                if (this._visible != value)
                {
                    OnVisibleChanging();
                    this._visible = value;
                    OnVisibleChanged();
                }
            }
        }

        protected delegate void RenderDelegate(Surface dst, Point offset);

        /// <summary>
        /// Render at the appropriate offset point
        /// </summary>
        public abstract void Render(Surface dst, Point offset);

        protected virtual void OnInvalidate(Rectangle rect)
        {
            this.OwnerList.Invalidate(rect);
        }

        public void Invalidate(Rectangle rect)
        {
            OnInvalidate(rect);
        }

        public void Invalidate(RectangleF rectF)
        {
            Rectangle rect = Utility.RoundRectangle(rectF);
            Invalidate(rect);
        }

        public void Invalidate(GeometryRegion region)
        {
            foreach (Rectangle rect in region.GetRegionScansReadOnlyInt())
            {
                Invalidate(rect);
            }
        }

        public void Invalidate()
        {
            Invalidate(Rectangle.FromLTRB(MinXCoordinate, MinYCoordinate, MaxXCoordinate + 1, MaxYCoordinate + 1));
        }

        public SurfaceBoxRenderer(SurfaceBoxRenderList ownerList)
        {
            this._ownerList = ownerList;
            this._visible = true;
        }

        ~SurfaceBoxRenderer()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            this._disposed = true;
        }
    }
}
