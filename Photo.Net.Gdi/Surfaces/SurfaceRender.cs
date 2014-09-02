using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Photo.Net.Base.Infomation;
using Photo.Net.Core;
using Photo.Net.Core.Color;
using ThreadPool = Photo.Net.Base.Thread.ThreadPool;

namespace Photo.Net.Gdi.Surfaces
{
    /// <summary>
    /// Renders a Surface to the screen.
    /// </summary>
    public sealed class SurfaceRender
        : Control
    {
        //        public const int MaxSideLength = 32767;
        //
        //        /// <summary>
        //        //  when this is non-zero, we just paint white (startup optimization)
        //        /// </summary>
        //        private int _justPaintWhite;
        //        private ScaleFactor _scaleFactor;
        //        private readonly ThreadPool _threadPool = new ThreadPool();
        //        private readonly SurfaceRendererList _rendererList;
        //        private SurfaceBoxBaseRenderer _baseRenderer;
        //        private Surface _surface;
        //
        //        // Each concrete instance of SurfaceBox holds a strong reference to a single
        //        // double buffer Surface. Statically, we store a weak reference to it. This way
        //        // when there are no more SurfaceBox instances, the double buffer Surface can
        //        // be cleaned up by the GC.
        //        private static WeakReference<Surface> _doubleBufferSurfaceWeakRef;
        //        private Surface _doubleBufferSurface;
        //
        //        public SurfaceRendererList RendererList
        //        {
        //            get
        //            {
        //                return this._rendererList;
        //            }
        //        }
        //
        //        public Surface Surface
        //        {
        //            get
        //            {
        //                return this._surface;
        //            }
        //
        //            set
        //            {
        //                this._surface = value;
        //                this._baseRenderer.Source = value;
        //
        //                if (this._surface != null)
        //                {
        //                    // Maintain the scalefactor
        //                    this.Size = this._scaleFactor.ScaleSize(_surface.Size);
        //                    this._rendererList.SourceSize = this._surface.Size;
        //                    this._rendererList.DestinationSize = this.Size;
        //                }
        //
        //                Invalidate();
        //            }
        //        }
        //
        //        [Obsolete("This functionality was moved to the DocumentView class", true)]
        //        public bool DrawGrid
        //        {
        //            get
        //            {
        //                return false;
        //            }
        //
        //            set
        //            {
        //            }
        //        }
        //
        //        public void FitToSize(Size fit)
        //        {
        //            ScaleFactor newSF = ScaleFactor.Min(fit.Width, _surface.Width,
        //                                                fit.Height, _surface.Height,
        //                                                ScaleFactor.MinValue);
        //
        //            this._scaleFactor = newSF;
        //            this.Size = this._scaleFactor.ScaleSize(_surface.Size);
        //        }
        //
        //        public void RenderTo(Surface dst)
        //        {
        //            dst.Clear(ColorBgra.Transparent);
        //
        //            if (this._surface != null)
        //            {
        //                SurfaceRendererList sbrl = new SurfaceRendererList(this._surface.Size, dst.Size);
        //                SurfaceBoxBaseRenderer sbbr = new SurfaceBoxBaseRenderer(sbrl, this._surface);
        //                sbrl.Add(sbbr, true);
        //                sbrl.Render(dst, new Point(0, 0));
        //                sbrl.Remove(sbbr);
        //            }
        //        }
        //
        //        /// <summary>
        //        /// Increments the "just paint white" counter. When this counter is non-zero,
        //        /// the OnPaint() method will only paint white. This is used as an optimization
        //        /// during Paint.NET's startup so that it doesn't have to touch all the pages
        //        /// of the blank document's layer.
        //        /// </summary>
        //        public void IncrementJustPaintWhite()
        //        {
        //            ++this._justPaintWhite;
        //        }
        //
        //        protected override void OnResize(EventArgs e)
        //        {
        //            base.OnResize(e);
        //
        //            // This code fixes the size of the surfaceBox as necessary to 
        //            // maintain the aspect ratio of the surface. Keeping the mouse
        //            // within 32767 is delegated to the new overflow-checking code
        //            // in Tool.cs.
        //
        //            Size mySize = this.Size;
        //            if (this.Width == MaxSideLength && _surface != null)
        //            {
        //                // Windows forms probably clamped this control's width, so we have to fix the height.
        //                mySize.Height = (int)(((long)(MaxSideLength + 1) * (long)_surface.Height) / (long)_surface.Width);
        //            }
        //            else if (mySize.Width == 0)
        //            {
        //                mySize.Width = 1;
        //            }
        //
        //            if (this.Width == MaxSideLength && _surface != null)
        //            {
        //                // Windows forms probably clamped this control's height, so we have to fix the width.
        //                mySize.Width = (int)(((long)(MaxSideLength + 1) * (long)_surface.Width) / (long)_surface.Height);
        //            }
        //            else if (mySize.Height == 0)
        //            {
        //                mySize.Height = 1;
        //            }
        //
        //            if (mySize != this.Size)
        //            {
        //                this.Size = mySize;
        //            }
        //
        //            if (_surface == null)
        //            {
        //                this._scaleFactor = ScaleFactor.OneToOne;
        //            }
        //            else
        //            {
        //                ScaleFactor newSF = ScaleFactor.Max(this.Width, _surface.Width,
        //                                                    this.Height, _surface.Height,
        //                                                    ScaleFactor.OneToOne);
        //
        //                this._scaleFactor = newSF;
        //            }
        //
        //            this._rendererList.DestinationSize = this.Size;
        //        }
        //
        //        public ScaleFactor ScaleFactor
        //        {
        //            get
        //            {
        //                return this._scaleFactor;
        //            }
        //        }
        //
        //        public SurfaceRender()
        //        {
        //            InitializeComponent();
        //            this._scaleFactor = ScaleFactor.OneToOne;
        //
        //            this._rendererList = new SurfaceRendererList(this.Size, this.Size);
        //            this._rendererList.Invalidated += new InvalidateEventHandler(Renderers_Invalidated);
        //            this._baseRenderer = new SurfaceBoxBaseRenderer(this._rendererList, null);
        //            this._rendererList.Add(this._baseRenderer, false);
        //        }
        //
        //        protected override void Dispose(bool disposing)
        //        {
        //            if (disposing)
        //            {
        //                if (this._baseRenderer != null)
        //                {
        //                    this._rendererList.Remove(this._baseRenderer);
        //                    this._baseRenderer.Dispose();
        //                    this._baseRenderer = null;
        //                }
        //
        //                if (this._doubleBufferSurface != null)
        //                {
        //                    this._doubleBufferSurface.Dispose();
        //                    this._doubleBufferSurface = null;
        //                }
        //            }
        //
        //            base.Dispose(disposing);
        //        }
        //
        //        /// <summary>
        //        /// This event is raised after painting has been performed. This is required because
        //        /// the normal Paint event is raised *before* painting has been performed.
        //        /// </summary>
        //        public event PaintEventHandler2 Painted;
        //        private void OnPainted(PaintEventArgs2 e)
        //        {
        //            if (Painted != null)
        //            {
        //                Painted(this, e);
        //            }
        //        }
        //
        //        public event PaintEventHandler2 PrePaint;
        //        private void OnPrePaint(PaintEventArgs2 e)
        //        {
        //            if (PrePaint != null)
        //            {
        //                PrePaint(this, e);
        //            }
        //        }
        //
        //        private Surface GetDoubleBuffer(Size size)
        //        {
        //            Surface localDBSurface = null;
        //            Size oldSize = new Size(0, 0);
        //
        //            // If we already have a double buffer surface reference, but if that surface
        //            // is already disposed then don't worry about it.
        //            if (this._doubleBufferSurface != null && this._doubleBufferSurface.IsDisposed)
        //            {
        //                oldSize = this._doubleBufferSurface.Size;
        //                this._doubleBufferSurface = null;
        //            }
        //
        //            // If we already have a double buffer surface reference, but if that surface
        //            // is too small, then nuke it.
        //            if (this._doubleBufferSurface != null &&
        //                (this._doubleBufferSurface.Width < size.Width || this._doubleBufferSurface.Height < size.Height))
        //            {
        //                oldSize = this._doubleBufferSurface.Size;
        //                this._doubleBufferSurface.Dispose();
        //                this._doubleBufferSurface = null;
        //                _doubleBufferSurfaceWeakRef = null;
        //            }
        //
        //            // If we don't have a double buffer, then we'd better get one.
        //            if (this._doubleBufferSurface != null)
        //            {
        //                // Got one!
        //                localDBSurface = this._doubleBufferSurface;
        //            }
        //            else if (_doubleBufferSurfaceWeakRef != null)
        //            {
        //                // First, try to get the one that's already shared amongst all SurfaceBox instances.
        //                localDBSurface = _doubleBufferSurfaceWeakRef.Target;
        //
        //                // If it's disposed, then forget about it.
        //                if (localDBSurface != null && localDBSurface.IsDisposed)
        //                {
        //                    oldSize = localDBSurface.Size;
        //                    localDBSurface = null;
        //                    _doubleBufferSurfaceWeakRef = null;
        //                }
        //            }
        //
        //            // Make sure the surface is big enough.
        //            if (localDBSurface != null && (localDBSurface.Width < size.Width || localDBSurface.Height < size.Height))
        //            {
        //                oldSize = localDBSurface.Size;
        //                localDBSurface.Dispose();
        //                localDBSurface = null;
        //                _doubleBufferSurfaceWeakRef = null;
        //            }
        //
        //            // So, do we have a surface? If not then we'd better make one.
        //            if (localDBSurface == null)
        //            {
        //                Size newSize = new Size(Math.Max(size.Width, oldSize.Width), Math.Max(size.Height, oldSize.Height));
        //                localDBSurface = new Surface(newSize.Width, newSize.Height);
        //                _doubleBufferSurfaceWeakRef = new WeakReference<Surface>(localDBSurface);
        //            }
        //
        //            this._doubleBufferSurface = localDBSurface;
        //            Surface window = localDBSurface.CreateWindow(0, 0, size.Width, size.Height);
        //            return window;
        //        }
        //
        //        protected override void OnPaint(PaintEventArgs e)
        //        {
        //            if (this._surface != null)
        //            {
        //                PdnRegion clipRegion = null;
        //                Rectangle[] rects = this._realUpdateRects;
        //
        //                if (rects == null)
        //                {
        //                    clipRegion = new PdnRegion(e.Graphics.Clip, true);
        //                    clipRegion.Intersect(e.ClipRectangle);
        //                    rects = clipRegion.GetRegionScansReadOnlyInt();
        //                }
        //
        //                if (this._justPaintWhite > 0)
        //                {
        //                    PdnGraphics.FillRectangles(e.Graphics, System.Drawing.Color.White, rects);
        //                }
        //                else
        //                {
        //                    foreach (Rectangle rect in rects)
        //                    {
        //                        if (e.Graphics.IsVisible(rect))
        //                        {
        //                            PaintEventArgs2 e2 = new PaintEventArgs2(e.Graphics, rect);
        //                            OnPaintImpl(e2);
        //                        }
        //                    }
        //                }
        //
        //                if (clipRegion != null)
        //                {
        //                    clipRegion.Dispose();
        //                    clipRegion = null;
        //                }
        //            }
        //
        //            if (this._justPaintWhite > 0)
        //            {
        //                --this._justPaintWhite;
        //            }
        //
        //            base.OnPaint(e);
        //        }
        //
        //        private void OnPaintImpl(PaintEventArgs2 e)
        //        {
        //            using (Surface doubleBuffer = GetDoubleBuffer(e.ClipRectangle.Size))
        //            {
        //                using (RenderArgs renderArgs = new RenderArgs(doubleBuffer))
        //                {
        //                    OnPrePaint(e);
        //                    DrawArea(renderArgs, e.ClipRectangle.Location);
        //                    OnPainted(e);
        //
        //                    IntPtr tracking;
        //                    Point childOffset;
        //                    Size parentSize;
        //                    doubleBuffer.GetDrawBitmapInfo(out tracking, out childOffset, out parentSize);
        //
        //                    PdnGraphics.DrawBitmap(e.Graphics, e.ClipRectangle, e.Graphics.Transform,
        //                        tracking, childOffset.X, childOffset.Y);
        //                }
        //            }
        //        }
        //
        //        protected override void OnPaintBackground(PaintEventArgs pevent)
        //        {
        //            // do nothing so as to avoid flicker
        //            // tip: for debugging, uncomment the next line!
        //            //base.OnPaintBackground(pevent);
        //        }
        //
        //        private class RenderContext
        //        {
        //            public Surface[] windows;
        //            public Point[] offsets;
        //            public Rectangle[] rects;
        //            public SurfaceRender owner;
        //            public WaitCallback waitCallback;
        //
        //            public void RenderThreadMethod(object indexObject)
        //            {
        //                int index = (int)indexObject;
        //                this.owner.rendererList.Render(windows[index], offsets[index]);
        //                this.windows[index].Dispose();
        //                this.windows[index] = null;
        //            }
        //        }
        //
        //        private RenderContext _renderContext;
        //
        //        /// <summary>
        //        /// Draws an area of the SurfaceBox.
        //        /// </summary>
        //        /// <param name="ra">The rendering surface object to draw to.</param>
        //        /// <param name="offset">The virtual offset of ra, in client (destination) coordinates.</param>
        //        /// <remarks>
        //        /// If drawing to ra.Surface or ra.Bitmap, copy the roi of the source surface to (0,0) of ra.Surface or ra.Bitmap
        //        /// If drawing to ra.Graphics, copy the roi of the surface to (roi.X, roi.Y) of ra.Graphics
        //        /// </remarks>
        //        private void DrawArea(RenderArgs ra, Point offset)
        //        {
        //            if (_surface == null)
        //            {
        //                return;
        //            }
        //
        //            if (_renderContext == null || (_renderContext.windows != null && _renderContext.windows.Length != Processor.LogicalCpuCount))
        //            {
        //                _renderContext = new RenderContext { owner = this };
        //                _renderContext.waitCallback = new WaitCallback(_renderContext.RenderThreadMethod);
        //                _renderContext.windows = new Surface[Processor.LogicalCpuCount];
        //                _renderContext.offsets = new Point[Processor.LogicalCpuCount];
        //                _renderContext.rects = new Rectangle[Processor.LogicalCpuCount];
        //            }
        //
        //            Utility.SplitRectangle(ra.Bounds, _renderContext.rects);
        //
        //            for (int i = 0; i < _renderContext.rects.Length; ++i)
        //            {
        //                if (_renderContext.rects[i].Width > 0 && _renderContext.rects[i].Height > 0)
        //                {
        //                    _renderContext.offsets[i] = new Point(_renderContext.rects[i].X + offset.X, _renderContext.rects[i].Y + offset.Y);
        //                    _renderContext.windows[i] = ra.Surface.CreateWindow(_renderContext.rects[i]);
        //                }
        //                else
        //                {
        //                    _renderContext.windows[i] = null;
        //                }
        //            }
        //
        //            for (int i = 0; i < _renderContext.windows.Length; ++i)
        //            {
        //                if (_renderContext.windows[i] != null)
        //                {
        //                    this._threadPool.QueueTask(_renderContext.waitCallback, BoxedConstants.GetInt32(i));
        //                }
        //            }
        //
        //            this._threadPool.Drain();
        //        }
        //
        //        private Rectangle[] _realUpdateRects;
        //        protected override void WndProc(ref Message m)
        //        {
        //            IntPtr preR = m.Result;
        //
        //            // Ignore focus
        //            if (m.Msg == 7 /* WM_SETFOCUS */)
        //            {
        //                return;
        //            }
        //            else if (m.Msg == 0x000f /* WM_PAINT */)
        //            {
        //                this._realUpdateRects = UI.GetUpdateRegion(this);
        //
        //                if (this._realUpdateRects != null &&
        //                    this._realUpdateRects.Length >= 5) // '5' chosen arbitrarily
        //                {
        //                    this._realUpdateRects = null;
        //                }
        //
        //                base.WndProc(ref m);
        //            }
        //            else
        //            {
        //                base.WndProc(ref m);
        //            }
        //        }
        //
        //        #region Transformation point
        //
        //        /// <summary>
        //        /// Converts from control client coordinates to surface coordinates
        //        /// This is useful when this.Bounds != surface.Bounds (i.e. some sort of zooming is in effect)
        //        /// </summary>
        //        /// <param name="clientPt"></param>
        //        /// <returns></returns>
        //        public PointF ClientToSurface(PointF clientPt)
        //        {
        //            return ScaleFactor.UnscalePoint(clientPt);
        //        }
        //
        //        public Point ClientToSurface(Point clientPt)
        //        {
        //            return ScaleFactor.UnscalePoint(clientPt);
        //        }
        //
        //        public SizeF ClientToSurface(SizeF clientSize)
        //        {
        //            return ScaleFactor.UnscaleSize(clientSize);
        //        }
        //
        //        public Size ClientToSurface(Size clientSize)
        //        {
        //            return Size.Round(ClientToSurface((SizeF)clientSize));
        //        }
        //
        //        public RectangleF ClientToSurface(RectangleF clientRect)
        //        {
        //            return new RectangleF(ClientToSurface(clientRect.Location), ClientToSurface(clientRect.Size));
        //        }
        //
        //        public Rectangle ClientToSurface(Rectangle clientRect)
        //        {
        //            return new Rectangle(ClientToSurface(clientRect.Location), ClientToSurface(clientRect.Size));
        //        }
        //
        //        public PointF SurfaceToClient(PointF surfacePt)
        //        {
        //            return ScaleFactor.ScalePoint(surfacePt);
        //        }
        //
        //        public Point SurfaceToClient(Point surfacePt)
        //        {
        //            return ScaleFactor.ScalePoint(surfacePt);
        //        }
        //
        //        public SizeF SurfaceToClient(SizeF surfaceSize)
        //        {
        //            return ScaleFactor.ScaleSize(surfaceSize);
        //        }
        //
        //        public Size SurfaceToClient(Size surfaceSize)
        //        {
        //            return Size.Round(SurfaceToClient((SizeF)surfaceSize));
        //        }
        //
        //        #endregion
        //
        //        public RectangleF SurfaceToClient(RectangleF surfaceRect)
        //        {
        //            return new RectangleF(SurfaceToClient(surfaceRect.Location), SurfaceToClient(surfaceRect.Size));
        //        }
        //
        //        public Rectangle SurfaceToClient(Rectangle surfaceRect)
        //        {
        //            return new Rectangle(SurfaceToClient(surfaceRect.Location), SurfaceToClient(surfaceRect.Size));
        //        }
        //
        //        /// <summary> 
        //        /// Required method for Designer support - do not modify 
        //        /// the contents of this method with the code editor.
        //        /// </summary>
        //        private void InitializeComponent()
        //        {
        //        }
        //
        //        private void Renderers_Invalidated(object sender, InvalidateEventArgs e)
        //        {
        //            Rectangle rect = SurfaceToClient(e.InvalidRect);
        //            rect.Inflate(1, 1);
        //            Invalidate(rect);
        //        }
    }
}