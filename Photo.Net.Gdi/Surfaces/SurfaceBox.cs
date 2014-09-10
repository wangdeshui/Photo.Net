using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Photo.Net.Base;
using Photo.Net.Base.Infomation;
using Photo.Net.Core;
using Photo.Net.Core.Area;
using Photo.Net.Core.Color;
using Photo.Net.Gdi.Event;
using Photo.Net.Gdi.Graphic;
using ThreadPool = Photo.Net.Base.Thread.ThreadPool;

namespace Photo.Net.Gdi.Surfaces
{
    /// <summary>
    /// Represent a Surface control, but it don't handle render.
    /// </summary>
    public sealed class SurfaceBox
        : Control
    {
        #region Property

        public const int MaxSideLength = 32767;

        /// <summary>
        //  when this is non-zero, we just paint white (startup optimization)
        /// </summary>
        private int _justPaintWhite;

        private ScaleFactor _scaleFactor;
        private readonly ThreadPool _threadPool = new ThreadPool();
        private readonly SurfaceBoxRenderList _rendererList;

        private RenderContext _renderContext;

        private Rectangle[] _realUpdateRects;

        private Surface _surface;
        private SurfaceBoxBaseRenderer _baseRenderer;

        private static WeakReference<Surface> _doubleBufferSurfaceWeakRef;
        private Surface _doubleBufferSurface;

        public Surface Surface
        {
            get
            {
                return this._surface;
            }

            set
            {
                this._surface = value;
                this._baseRenderer.Source = value;

                if (this._surface != null)
                {
                    // Maintain the scalefactor
                    this.Size = this._scaleFactor.ScaleSize(_surface.Size);
                    this._rendererList.SourceSize = this._surface.Size;
                    this._rendererList.DestinationSize = this.Size;
                }

                Invalidate();
            }
        }

        public SurfaceBoxRenderList RendererList
        {
            get
            {
                return this._rendererList;
            }
        }

        #endregion

        public void RenderTo(Surface dst)
        {
            dst.Clear(ColorBgra.Transparent);

            if (this._surface != null)
            {
                var sbrl = new SurfaceBoxRenderList(this._surface.Size, dst.Size);
                var sbbr = new SurfaceBoxBaseRenderer(sbrl, this._surface);
                sbrl.Add(sbbr, true);
                sbrl.Render(dst, new Point(0, 0));
                sbrl.Remove(sbbr);
            }
        }

        public void FitToSize(Size fit)
        {
            ScaleFactor newSF = ScaleFactor.Min(fit.Width, _surface.Width,
                                                fit.Height, _surface.Height,
                                                ScaleFactor.MinValue);

            this._scaleFactor = newSF;
            this.Size = this._scaleFactor.ScaleSize(_surface.Size);
        }

        /// <summary>
        /// Increments the "just paint white" counter. When this counter is non-zero,
        /// the OnPaint() method will only paint white. This is used as an optimization
        /// during Paint.NET's startup so that it doesn't have to touch all the pages
        /// of the blank document's layer.
        /// </summary>
        public void IncrementJustPaintWhite()
        {
            ++this._justPaintWhite;
        }

        public SurfaceBox()
        {
            this._scaleFactor = ScaleFactor.OneToOne;

            this._rendererList = new SurfaceBoxRenderList(this.Size, this.Size);
            this._rendererList.Invalidated += Renderers_Invalidated;
            this._baseRenderer = new SurfaceBoxBaseRenderer(this._rendererList, null);
            this._rendererList.Add(this._baseRenderer, false);

            this.DoubleBuffered = true;
        }

        #region Events

        /// <summary>
        /// This event is raised after painting has been performed. This is required because
        /// the normal Paint event is raised *before* painting has been performed.
        /// </summary>
        public event DrawEventHandler Painted;

        private void OnPainted(DrawArgs e)
        {
            if (Painted != null)
            {
                Painted(this, e);
            }
        }

        public event DrawEventHandler Painting;

        private void OnPainting(DrawArgs e)
        {
            if (Painting != null)
            {
                Painting(this, e);
            }
        }

        #endregion

        #region Double Buffer

        private Surface GetDoubleBuffer(Size size)
        {
            Surface localDbSurface = null;
            var oldSize = new Size(0, 0);

            // If we already have a double buffer surface reference, but if that surface
            // is already disposed then don't worry about it.
            if (this._doubleBufferSurface != null && this._doubleBufferSurface.IsDisposed)
            {
                oldSize = this._doubleBufferSurface.Size;
                this._doubleBufferSurface = null;
            }

            // If we already have a double buffer surface reference, but if that surface
            // is too small, then nuke it.
            if (this._doubleBufferSurface != null &&
                (this._doubleBufferSurface.Width < size.Width || this._doubleBufferSurface.Height < size.Height))
            {
                oldSize = this._doubleBufferSurface.Size;
                this._doubleBufferSurface.Dispose();
                this._doubleBufferSurface = null;
                _doubleBufferSurfaceWeakRef = null;
            }

            // If we don't have a double buffer, then we'd better get one.
            if (this._doubleBufferSurface != null)
            {
                localDbSurface = this._doubleBufferSurface;
            }
            else if (_doubleBufferSurfaceWeakRef != null)
            {
                if (_doubleBufferSurfaceWeakRef.TryGetTarget(out localDbSurface))
                {
                    // If it's disposed, then forget about it.
                    if (localDbSurface.IsDisposed)
                    {
                        oldSize = localDbSurface.Size;
                        localDbSurface = null;
                        _doubleBufferSurfaceWeakRef = null;
                    }
                }

            }

            // Make sure the surface is big enough.
            if (localDbSurface != null && (localDbSurface.Width < size.Width || localDbSurface.Height < size.Height))
            {
                oldSize = localDbSurface.Size;
                localDbSurface.Dispose();
                localDbSurface = null;
                _doubleBufferSurfaceWeakRef = null;
            }

            // So, do we have a surface? If not then we'd better make one.
            if (localDbSurface == null)
            {
                var newSize = new Size(Math.Max(size.Width, oldSize.Width), Math.Max(size.Height, oldSize.Height));
                localDbSurface = new Surface(newSize.Width, newSize.Height);
                _doubleBufferSurfaceWeakRef = new WeakReference<Surface>(localDbSurface);
            }

            this._doubleBufferSurface = localDbSurface;
            Surface window = localDbSurface.CreateWindow(0, 0, size.Width, size.Height);
            return window;
        }

        #endregion

        #region Window Proc

        protected override void WndProc(ref Message m)
        {

            switch (m.Msg)
            {
                // Ignore focus
                case 7: return;
                // WM_PAINT
                case 0x000f:
                    this._realUpdateRects = GetUpdateRegion(this);

                    if (this._realUpdateRects != null &&
                        this._realUpdateRects.Length >= 5) // '5' chosen arbitrarily
                    {
                        this._realUpdateRects = null;
                    }
                    break;
            }

            base.WndProc(ref m);
        }

        public static Rectangle[] GetUpdateRegion(Control control)
        {
            SafeNativeMethods.GetUpdateRgn(control.Handle, UserInterface.HRgn, false);
            Rectangle[] scans;
            int area;
            PtnGraphics.GetRegionScans(UserInterface.HRgn, out scans, out area);
            GC.KeepAlive(control);
            return scans;
        }

        #endregion

        #region Transformation point

        /// <summary>
        /// Converts from control client coordinates to surface coordinates
        /// This is useful when this.Bounds != surface.Bounds (i.e. some sort of zooming is in effect)
        /// </summary>
        /// <param name="clientPt"></param>
        /// <returns></returns>
        public PointF ClientToSurface(PointF clientPt)
        {
            return ScaleFactor.UnscalePoint(clientPt);
        }

        public Point ClientToSurface(Point clientPt)
        {
            return ScaleFactor.UnscalePoint(clientPt);
        }

        public SizeF ClientToSurface(SizeF clientSize)
        {
            return ScaleFactor.UnscaleSize(clientSize);
        }

        public Size ClientToSurface(Size clientSize)
        {
            return Size.Round(ClientToSurface((SizeF)clientSize));
        }

        public RectangleF ClientToSurface(RectangleF clientRect)
        {
            return new RectangleF(ClientToSurface(clientRect.Location), ClientToSurface(clientRect.Size));
        }

        public Rectangle ClientToSurface(Rectangle clientRect)
        {
            return new Rectangle(ClientToSurface(clientRect.Location), ClientToSurface(clientRect.Size));
        }

        public PointF SurfaceToClient(PointF surfacePt)
        {
            return ScaleFactor.ScalePoint(surfacePt);
        }

        public Point SurfaceToClient(Point surfacePt)
        {
            return ScaleFactor.ScalePoint(surfacePt);
        }

        public SizeF SurfaceToClient(SizeF surfaceSize)
        {
            return ScaleFactor.ScaleSize(surfaceSize);
        }

        public Size SurfaceToClient(Size surfaceSize)
        {
            return Size.Round(SurfaceToClient((SizeF)surfaceSize));
        }

        public RectangleF SurfaceToClient(RectangleF surfaceRect)
        {
            return new RectangleF(SurfaceToClient(surfaceRect.Location), SurfaceToClient(surfaceRect.Size));
        }

        public Rectangle SurfaceToClient(Rectangle surfaceRect)
        {
            return new Rectangle(SurfaceToClient(surfaceRect.Location), SurfaceToClient(surfaceRect.Size));
        }

        #endregion

        #region Resize and zoom

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            // This code fixes the size of the surfaceBox as necessary to 
            // maintain the aspect ratio of the surface. Keeping the mouse
            // within 32767 is delegated to the new overflow-checking code
            // in Tool.cs.

            Size mySize = this.Size;
            if (this.Width == MaxSideLength && _surface != null)
            {
                // Windows forms probably clamped this control's width, so we have to fix the height.
                mySize.Height = (int)(((long)(MaxSideLength + 1) * (long)_surface.Height) / (long)_surface.Width);
            }
            else if (mySize.Width == 0)
            {
                mySize.Width = 1;
            }

            if (this.Width == MaxSideLength && _surface != null)
            {
                // Windows forms probably clamped this control's height, so we have to fix the width.
                mySize.Width = (int)(((long)(MaxSideLength + 1) * (long)_surface.Width) / (long)_surface.Height);
            }
            else if (mySize.Height == 0)
            {
                mySize.Height = 1;
            }

            if (mySize != this.Size)
            {
                this.Size = mySize;
            }

            if (_surface == null)
            {
                this._scaleFactor = ScaleFactor.OneToOne;
            }
            else
            {
                ScaleFactor newSF = ScaleFactor.Max(this.Width, _surface.Width,
                    this.Height, _surface.Height,
                    ScaleFactor.OneToOne);

                this._scaleFactor = newSF;
            }

            this._rendererList.DestinationSize = this.Size;
        }

        public ScaleFactor ScaleFactor
        {
            get { return this._scaleFactor; }
        }

        #endregion

        #region Draw

        private void Renderers_Invalidated(object sender, InvalidateEventArgs e)
        {
            Rectangle rect = SurfaceToClient(e.InvalidRect);
            rect.Inflate(1, 1);
            Invalidate(rect);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (this._surface != null)
            {
                GeometryRegion clipRegion = null;
                Rectangle[] rects = this._realUpdateRects;

                if (rects == null)
                {
                    clipRegion = new GeometryRegion(e.Graphics.Clip, true);
                    clipRegion.Intersect(e.ClipRectangle);
                    rects = clipRegion.GetRegionScansReadOnlyInt();
                }

                if (this._justPaintWhite > 0)
                {
                    PtnGraphics.FillRectangles(e.Graphics, Color.White, rects);
                }
                else
                {
                    foreach (Rectangle rect in rects)
                    {
                        if (e.Graphics.IsVisible(rect))
                        {
                            var drawArg = new DrawArgs(e.Graphics, rect);
                            Draw(drawArg);
                        }
                    }
                }

                if (clipRegion != null)
                {
                    clipRegion.Dispose();
                }
            }

            if (this._justPaintWhite > 0)
            {
                --this._justPaintWhite;
            }

            base.OnPaint(e);
        }

        public void Draw(DrawArgs drawArg)
        {
            using (Surface doubleBuffer = GetDoubleBuffer(drawArg.ClipRectangle.Size))
            {
                using (var renderArgs = new RenderArgs(doubleBuffer))
                {
                    OnPainting(drawArg);

                    // Draw to buffer, use multiple thread, but will wait all thread completed and return.
                    DrawArea(renderArgs, drawArg.ClipRectangle.Location);

                    OnPainted(drawArg);

                    IntPtr bmpPtr;
                    Point childOffset;
                    Size parentSize;
                    doubleBuffer.GetDrawBitmapInfo(out bmpPtr, out childOffset, out parentSize);


                    //                    var img = new Bitmap(1024, 691, 32, PixelFormat.Format32bppArgb, bmpPtr);
                    //                    drawArg.Graphics.DrawImage(img, 0, 0);
                    // Draw to screen
                    PtnGraphics.DrawBitmap(drawArg.Graphics, drawArg.ClipRectangle, drawArg.Graphics.Transform,
                        bmpPtr, childOffset.X, childOffset.Y);
                }
            }
        }

        public void DrawArea(RenderArgs ra, Point offset)
        {
            if (_surface == null) return;

            if (_renderContext == null ||
                (_renderContext.Windows != null && _renderContext.Windows.Length != Processor.LogicalCpuCount))
            {
                _renderContext = new RenderContext { Owner = this };
                _renderContext.WaitCallback = _renderContext.RenderThreadMethod;
                _renderContext.Windows = new Surface[Processor.LogicalCpuCount];
                _renderContext.Offsets = new Point[Processor.LogicalCpuCount];
                _renderContext.Rects = new Rectangle[Processor.LogicalCpuCount];
            }

            Utility.SplitRectangle(ra.Bounds, _renderContext.Rects);



            for (int i = 0; i < _renderContext.Rects.Length; ++i)
            {
                var rect = _renderContext.Rects[i];
                if (rect.Width > 0 && rect.Height > 0)
                {
                    _renderContext.Offsets[i] = new Point(rect.X + offset.X, rect.Y + offset.Y);
                    if (_renderContext.Windows != null) _renderContext.Windows[i] = ra.Surface.CreateWindow(rect);
                }
                else
                {
                    if (_renderContext.Windows != null) _renderContext.Windows[i] = null;
                }
            }

            for (int i = 0; i < _renderContext.Windows.Length; ++i)
            {
                if (_renderContext.Windows[i] != null)
                {
                    this._threadPool.QueueTask(_renderContext.WaitCallback, BoxedConstants.GetInt32(i));
                }
            }

            this._threadPool.Drain();
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this._baseRenderer != null)
                {
                    this._rendererList.Remove(this._baseRenderer);
                    this._baseRenderer.Dispose();
                    this._baseRenderer = null;
                }

                if (this._doubleBufferSurface != null)
                {
                    this._doubleBufferSurface.Dispose();
                    this._doubleBufferSurface = null;
                }
            }

            base.Dispose(disposing);
        }

        private class RenderContext
        {
            public Surface[] Windows;
            public Point[] Offsets;
            public Rectangle[] Rects;
            public SurfaceBox Owner;
            public WaitCallback WaitCallback;

            public void RenderThreadMethod(object indexObject)
            {
                var index = (int)indexObject;
                this.Owner._rendererList.Render(Windows[index], Offsets[index]);
                this.Windows[index].Dispose();
                this.Windows[index] = null;
            }
        }
    }


}