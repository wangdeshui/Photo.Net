using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Photo.Net.Base;
using Photo.Net.Base.Delegate;
using Photo.Net.Core;
using Photo.Net.Core.Color;
using Photo.Net.Core.PixelOperation;
using Photo.Net.Core.Struct;
using Photo.Net.Gdi.Event;
using Photo.Net.Gdi.Surfaces;
using Photo.Net.Tool.Controls;
using Photo.Net.Tool.Events;
using Photo.Net.Tool.Layer;
using Photo.Net.Tool.Window;

namespace Photo.Net.Tool.Documents
{
    /// <summary>
    /// Encapsulates rendering the document by itself, including rulers and
    /// scrollbar decorators. It also raises events for mouse movement that
    /// are properly translated to (x,y) pixel coordinates within the document
    /// (DocumentMouse* events).
    /// </summary>
    public class DocumentView
        : BaseControl
    {

        #region Design code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new Container();
            this._topRuler = new Ruler();
            this._leftRuler = new Ruler();
            this._panel = new PanelEx();
            this._surfaceBox = new SurfaceBox();
            this._panel.SuspendLayout();
            this.SuspendLayout();
            // 
            // topRuler
            // 
            this._topRuler.BackColor = Color.White;
            this._topRuler.Dock = DockStyle.Top;
            this._topRuler.Location = new Point(0, 0);
            this._topRuler.Name = "_topRuler";
            this._topRuler.Offset = -16;
            this._topRuler.Size = UserInterface.ScaleSize(new Size(384, 16));
            this._topRuler.TabIndex = 3;
            // 
            // leftRuler
            // 
            this._leftRuler.BackColor = Color.White;
            this._leftRuler.Dock = DockStyle.Left;
            this._leftRuler.Location = new Point(0, 16);
            this._leftRuler.Name = "_leftRuler";
            this._leftRuler.Orientation = Orientation.Vertical;
            this._leftRuler.Size = UserInterface.ScaleSize(new Size(16, 304));
            this._leftRuler.TabIndex = 4;
            // 
            // panel
            // 
            this._panel.AutoScroll = true;
            this._panel.Controls.Add(this._surfaceBox);
            this._panel.Dock = DockStyle.Fill;
            this._panel.Location = new Point(16, 16);
            this._panel.Name = "_panel";
            this._panel.ScrollPosition = new Point(0, 0);
            this._panel.Size = new Size(368, 304);
            this._panel.TabIndex = 5;
            this._panel.Scroll += this.Panel_Scroll;
            this._panel.KeyDown += Panel_KeyDown;
            this._panel.KeyUp += Panel_KeyUp;
            this._panel.KeyPress += Panel_KeyPress;
            this._panel.GotFocus += Panel_GotFocus;
            this._panel.LostFocus += Panel_LostFocus;
            // 
            // surfaceBox
            // 
            this._surfaceBox.Location = new Point(0, 0);
            this._surfaceBox.Name = "_surfaceBox";
            this._surfaceBox.Surface = null;
            this._surfaceBox.TabIndex = 0;
            // 
            // DocumentView
            // 
            this.Controls.Add(this._panel);
            this.Controls.Add(this._leftRuler);
            this.Controls.Add(this._topRuler);
            this.Name = "DocumentView";
            this.Size = new Size(384, 320);
            this._panel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        #region Fields

        // rulers really are on by default, so 'true' was set to show this.
        private bool _rulersEnabled = true;

        private bool _raiseFirstInputAfterGotFocus;
        private int _refreshSuspended;

        private IContainer components;
        private Document _document;
        private Surface _compositionSurface;
        private Ruler _leftRuler;
        private PanelEx _panel;
        private Ruler _topRuler;
        private SurfaceBox _surfaceBox;
        private readonly SurfaceBoxGridRenderer _gridRenderer;
        private readonly ControlShadow _controlShadow;

        private FormWindowState _oldWindowState = FormWindowState.Minimized;

        public SurfaceBoxRenderList RendererList
        {
            get { return this._surfaceBox.RendererList; }
        }

        public void IncrementJustPaintWhite()
        {
            this._surfaceBox.IncrementJustPaintWhite();
        }

        public new BorderStyle BorderStyle
        {
            get { return this._panel.BorderStyle; }

            set { this._panel.BorderStyle = value; }
        }

        #endregion

        /// <summary>
        /// Initializes an instance of the DocumentView class.
        /// </summary>
        public DocumentView()
        {
            UserInterface.InitScaling(this);

            InitializeComponent();

            this._document = null;
            this._compositionSurface = null;

            this._controlShadow = new ControlShadow();
            this._controlShadow.OccludingControl = _surfaceBox;
            this._controlShadow.Paint += new PaintEventHandler(ControlShadow_Paint);
            this._panel.Controls.Add(_controlShadow);
            this._panel.Controls.SetChildIndex(_controlShadow, _panel.Controls.Count - 1);

            this._gridRenderer = new SurfaceBoxGridRenderer(this._surfaceBox.RendererList);
            this._gridRenderer.Visible = false;
            this._surfaceBox.RendererList.Add(this._gridRenderer, true);

            this._surfaceBox.RendererList.Invalidated += new InvalidateEventHandler(Renderers_Invalidated);

            this._panel.Select();
        }

        #region Load and dispose

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            InitRenderSurface();
        }

        private void InitRenderSurface()
        {
            if (this._compositionSurface == null && Document != null)
            {
                this._compositionSurface = new Surface(Document.Size);
            }
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.components != null)
                {
                    this.components.Dispose();
                    this.components = null;
                }

                if (this._compositionSurface != null)
                {
                    this._compositionSurface.Dispose();
                    this._compositionSurface = null;
                }
            }

            base.Dispose(disposing);
        }

        #endregion

        #region Grid

        public event EventHandler DrawGridChanged;
        protected virtual void OnDrawGridChanged()
        {
            if (DrawGridChanged != null)
            {
                DrawGridChanged(this, EventArgs.Empty);
            }
        }

        public bool DrawGrid
        {
            get
            {
                return this._gridRenderer.Visible;
            }

            set
            {
                if (this._gridRenderer.Visible != value)
                {
                    this._gridRenderer.Visible = value;
                    OnDrawGridChanged();
                }
            }
        }

        #endregion

        #region ZoomIn / ZoomOut

        public void ZoomToWindow()
        {
            if (this._document != null)
            {
                Rectangle max = ClientRectangleMax;

                ScaleFactor zoom = ScaleFactor.Min(max.Width - 10,
                    _document.Width,
                    max.Height - 10,
                    _document.Height,
                    ScaleFactor.MinValue);

                ScaleFactor min = ScaleFactor.Min(zoom, ScaleFactor.OneToOne);
                this.ScaleFactor = min;
            }
        }

        private double GetZoomInFactorEpsilon()
        {
            // Increase ratio by 1 percentage point
            double currentRatio = this.ScaleFactor.Ratio;
            double factor1 = (currentRatio + 0.01) / currentRatio;

            // Increase ratio so that we increase our view by 1 pixel
            double ratioW = (double)(_surfaceBox.Width + 1) / (double)_surfaceBox.Surface.Width;
            double ratioH = (double)(_surfaceBox.Height + 1) / (double)_surfaceBox.Surface.Height;
            double ratio = Math.Max(ratioW, ratioH);
            double factor2 = ratio / currentRatio;

            double factor = Math.Max(factor1, factor2);

            return factor;
        }

        private double GetZoomOutFactorEpsilon()
        {
            double ratio = this.ScaleFactor.Ratio;
            return (ratio - 0.01) / ratio;
        }

        public virtual void ZoomIn(double factor)
        {
            Do.TryBool(() => ZoomInImpl(factor));
        }

        private void ZoomInImpl(double factor)
        {
            PointF centerPt = this.DocumentCenterPointF;

            ScaleFactor oldSF = this.ScaleFactor;
            ScaleFactor newSF = this.ScaleFactor;
            int countdown = 3;

            // At a minimum we want to increase the size of visible document by 1 pixel
            // Figure out what the ratio of ourSize : ourSize+1 is, and start out with that
            double zoomInEps = GetZoomInFactorEpsilon();
            double desiredFactor = Math.Max(factor, zoomInEps);
            double newFactor = desiredFactor;

            // Keep setting the ScaleFactor until it actually 'sticks'
            // Important for certain image sizes where not all zoom levels create distinct
            // screen sizes
            do
            {
                newSF = ScaleFactor.FromDouble(newSF.Ratio * newFactor);
                this.ScaleFactor = newSF;
                --countdown;
                newFactor *= 1.10;
            } while (this.ScaleFactor == oldSF && countdown > 0);

            this.DocumentCenterPointF = centerPt;
        }

        public virtual void ZoomIn()
        {
            Do.TryBool(ZoomInImpl);
        }

        private void ZoomInImpl()
        {
            PointF centerPt = this.DocumentCenterPointF;

            ScaleFactor oldSF = this.ScaleFactor;
            ScaleFactor newSF = this.ScaleFactor;
            int countdown = ScaleFactor.PresetValues.Length;

            // Keep setting the ScaleFactor until it actually 'sticks'
            // Important for certain image sizes where not all zoom levels create distinct
            // screen sizes
            do
            {
                newSF = newSF.GetNextLarger();
                this.ScaleFactor = newSF;
                --countdown;
            } while (this.ScaleFactor == oldSF && countdown > 0);

            this.DocumentCenterPointF = centerPt;
        }

        public virtual void ZoomOut(double factor)
        {
            Do.TryBool(() => ZoomOutImpl(factor));
        }

        private void ZoomOutImpl(double factor)
        {
            PointF centerPt = this.DocumentCenterPointF;

            ScaleFactor oldSF = this.ScaleFactor;
            ScaleFactor newSF = this.ScaleFactor;
            int countdown = 3;

            // At a minimum we want to decrease the size of visible document by 1 pixel (without dividing by zero of course)
            // Figure out what the ratio of ourSize : ourSize-1 is, and start out with that
            double zoomOutEps = GetZoomOutFactorEpsilon();
            double factorRecip = 1.0 / factor;
            double desiredFactor = Math.Min(factorRecip, zoomOutEps);
            double newFactor = desiredFactor;

            // Keep setting the ScaleFactor until it actually 'sticks'
            // Important for certain image sizes where not all zoom levels create distinct
            // screen sizes
            do
            {
                newSF = ScaleFactor.FromDouble(newSF.Ratio * newFactor);
                this.ScaleFactor = newSF;
                --countdown;
                newFactor *= 0.9;
            } while (this.ScaleFactor == oldSF && countdown > 0);

            this.DocumentCenterPointF = centerPt;
        }

        public virtual void ZoomOut()
        {
            Do.TryBool(ZoomOutImpl);
        }

        private void ZoomOutImpl()
        {
            PointF centerPt = this.DocumentCenterPointF;

            ScaleFactor oldSF = this.ScaleFactor;
            ScaleFactor newSF = this.ScaleFactor;
            int countdown = ScaleFactor.PresetValues.Length;

            // Keep setting the ScaleFactor until it actually 'sticks'
            // Important for certain image sizes where not all zoom levels create distinct
            // screen sizes
            do
            {
                newSF = newSF.GetNextSmaller();
                this.ScaleFactor = newSF;
                --countdown;
            } while (this.ScaleFactor == oldSF && countdown > 0);

            this.DocumentCenterPointF = centerPt;
        }

        #endregion

        #region Scalefactor

        private ScaleFactor _scaleFactor = new ScaleFactor(1, 1);

        /// <summary>
        /// Gets the maximum scale factor that the current document may be displayed at.
        /// </summary>
        public ScaleFactor MaxScaleFactor
        {
            get
            {
                ScaleFactor maxSF;

                if (this._document.Width == 0 || this._document.Height == 0)
                {
                    maxSF = ScaleFactor.MaxValue;
                }
                else
                {
                    double maxHScale = (double)SurfaceBox.MaxSideLength / this._document.Width;
                    double maxVScale = (double)SurfaceBox.MaxSideLength / this._document.Height;
                    double maxScale = Math.Min(maxHScale, maxVScale);
                    maxSF = ScaleFactor.FromDouble(maxScale);
                }

                return maxSF;
            }
        }

        [Browsable(false)]
        public ScaleFactor ScaleFactor
        {
            get { return this._scaleFactor; }

            set
            {
                UserInterface.SuspendControlPainting(this);

                ScaleFactor newValue = ScaleFactor.Min(value, MaxScaleFactor);

                if (newValue == this._scaleFactor &&
                    this._scaleFactor == ScaleFactor.OneToOne)
                {
                    // this space intentionally left blank
                }
                else
                {
                    RectangleF visibleRect = this.VisibleDocumentRectangleF;
                    ScaleFactor oldSF = _scaleFactor;
                    _scaleFactor = newValue;

                    // This value is used later below to re-center the document on screen
                    PointF centerPt = new PointF(visibleRect.X + visibleRect.Width / 2,
                        visibleRect.Y + visibleRect.Height / 2);

                    if (_surfaceBox != null && _compositionSurface != null)
                    {
                        _surfaceBox.Size = Size.Truncate((SizeF)_scaleFactor.ScaleSize(_compositionSurface.Bounds.Size));
                        _scaleFactor = _surfaceBox.ScaleFactor;

                        if (_leftRuler != null)
                        {
                            this._leftRuler.ScaleFactor = _scaleFactor;
                        }

                        if (_topRuler != null)
                        {
                            this._topRuler.ScaleFactor = _scaleFactor;
                        }
                    }

                    // re center ourself
                    RectangleF visibleRect2 = this.VisibleDocumentRectangleF;
                    RecenterView(centerPt);
                }

                this.OnResize(EventArgs.Empty);
                this.OnScaleFactorChanged();

                UserInterface.ResumeControlPainting(this);
                Invalidate(true);
            }
        }


        public event EventHandler ScaleFactorChanged;
        protected virtual void OnScaleFactorChanged()
        {
            if (ScaleFactorChanged != null)
            {
                ScaleFactorChanged(this, EventArgs.Empty);
            }
        }


        public MeasurementUnit Units
        {
            get
            {
                //                return this.leftRuler.MeasurementUnit;
                return new MeasurementUnit();
            }

            set
            {
                OnUnitsChanging();
                //                this.leftRuler.MeasurementUnit = value;
                //                this.topRuler.MeasurementUnit = value;
                DocumentMetaDataChangedHandler(this, EventArgs.Empty);
                OnUnitsChanged();
            }
        }

        protected virtual void OnUnitsChanging()
        {
        }

        protected virtual void OnUnitsChanged()
        {
        }

        #endregion

        #region Point Conversion

        /// <summary>
        /// Converts a point from the Windows Forms "client" coordinate space (wrt the DocumentView)
        /// into the Document coordinate space.
        /// </summary>
        /// <param name="clientPt">A Point that is in our client coordinates.</param>
        /// <returns>A Point that is in Document coordinates.</returns>
        public PointF ClientToDocument(Point clientPt)
        {
            Point screen = PointToScreen(clientPt);
            Point sbClient = _surfaceBox.PointToClient(screen);
            return _surfaceBox.ClientToSurface(sbClient);
        }

        /// <summary>
        /// Converts a point from screen coordinates to document coordinates
        /// </summary>
        /// <param name="screen">The point in screen coordinates to convert to document coordinates</param>
        public PointF ScreenToDocument(PointF screen)
        {
            Point offset = _surfaceBox.PointToClient(new Point(0, 0));
            return _surfaceBox.ClientToSurface(new PointF(screen.X + (float)offset.X, screen.Y + (float)offset.Y));
        }

        /// <summary>
        /// Converts a point from screen coordinates to document coordinates
        /// </summary>
        /// <param name="screen">The point in screen coordinates to convert to document coordinates</param>
        public Point ScreenToDocument(Point screen)
        {
            Point offset = _surfaceBox.PointToClient(new Point(0, 0));
            return _surfaceBox.ClientToSurface(new Point(screen.X + offset.X, screen.Y + offset.Y));
        }

        /// <summary>
        /// Converts a PointF from the RealTimeStylus coordinate space
        /// into the Document coordinate space.
        /// </summary>
        /// <param name="clientPt">A Point that is in RealTimeStylus coordinate space.</param>
        /// <returns>A Point that is in Document coordinates.</returns>
        public PointF ClientToSurface(PointF clientPt)
        {
            return _surfaceBox.ClientToSurface(clientPt);
        }

        /// <summary>
        /// Converts a point from Document coordinate space into the Windows Forms "client"
        /// coordinate space.
        /// </summary>
        /// <param name="clientPt">A Point that is in Document coordinates.</param>
        /// <returns>A Point that is in client coordinates.</returns>
        public PointF DocumentToClient(PointF documentPt)
        {
            PointF sbClient = _surfaceBox.SurfaceToClient(documentPt);
            Point screen = _surfaceBox.PointToScreen(Point.Round(sbClient));
            return PointToClient(screen);
        }

        /// <summary>
        /// Converts a rectangle from the Windows Forms "client" coordinate space into the Document
        /// coordinate space.
        /// </summary>
        /// <param name="clientPt">A Rectangle that is in client coordinates.</param>
        /// <returns>A Rectangle that is in Document coordinates.</returns>
        public RectangleF ClientToDocument(Rectangle clientRect)
        {
            Rectangle screen = RectangleToScreen(clientRect);
            Rectangle sbClient = _surfaceBox.RectangleToClient(screen);
            return _surfaceBox.ClientToSurface((RectangleF)sbClient);
        }

        /// <summary>
        /// Converts a rectangle from Document coordinate space into the Windows Forms "client"
        /// coordinate space.
        /// </summary>
        /// <param name="clientPt">A Rectangle that is in Document coordinates.</param>
        /// <returns>A Rectangle that is in client coordinates.</returns>
        public RectangleF DocumentToClient(RectangleF documentRect)
        {
            RectangleF sbClient = _surfaceBox.SurfaceToClient(documentRect);
            Rectangle screen = _surfaceBox.RectangleToScreen(Utility.RoundRectangle(sbClient));
            return RectangleToClient(screen);
        }

        private Rectangle SurfaceBoxToControlShadow(Rectangle rect)
        {
            Rectangle screenRect = this._surfaceBox.RectangleToScreen(rect);
            Rectangle csRect = this._controlShadow.RectangleToClient(screenRect);
            return csRect;
        }

        public PointF MouseToDocumentF(Control sender, Point mouse)
        {
            Point screenPoint = sender.PointToScreen(mouse);
            Point sbClient = _surfaceBox.PointToClient(screenPoint);

            PointF docPoint = _surfaceBox.ClientToSurface(new PointF(sbClient.X, sbClient.Y));

            return docPoint;
        }

        public Point MouseToDocument(Control sender, Point mouse)
        {
            Point screenPoint = sender.PointToScreen(mouse);
            Point sbClient = _surfaceBox.PointToClient(screenPoint);

            // Note: We're intentionally making this truncate instead of rounding so that
            // when the image is zoomed in, the proper pixel is affected
            Point docPoint = Point.Truncate(_surfaceBox.ClientToSurface(sbClient));

            return docPoint;
        }

        public Rectangle ClientRectangleMax
        {
            get
            {
                return RectangleToClient(this._panel.RectangleToScreen(this._panel.Bounds));
            }
        }

        public Rectangle ClientRectangleMin
        {
            get
            {
                Rectangle bounds = ClientRectangleMax;
                bounds.Width -= SystemInformation.VerticalScrollBarWidth;
                bounds.Height -= SystemInformation.HorizontalScrollBarHeight;
                return bounds;
            }
        }

        #endregion

        #region Visible View Area

        /// <summary>
        /// Returns a rectangle for the bounding rectangle of what is currently visible on screen,
        /// in document coordinates.
        /// </summary>
        [Browsable(false)]
        public RectangleF VisibleDocumentRectangleF
        {
            get
            {
                Rectangle panelRect = _panel.RectangleToScreen(_panel.ClientRectangle); // screen coords
                Rectangle surfaceBoxRect = _surfaceBox.RectangleToScreen(_surfaceBox.ClientRectangle); // screen coords
                Rectangle docScreenRect = Rectangle.Intersect(panelRect, surfaceBoxRect); // screen coords
                Rectangle docClientRect = RectangleToClient(docScreenRect);
                RectangleF docDocRectF = ClientToDocument(docClientRect);
                return docDocRectF;
            }
        }

        /// <summary>
        /// Returns a rectangle in <b>screen</b> coordinates that represents the space taken up
        /// by the document that is visible on screen.
        /// </summary>
        [Browsable(false)]
        public Rectangle VisibleDocumentBounds
        {
            get
            {
                // convert coordinates: document -> client -> screen
                return RectangleToScreen(Utility.RoundRectangle(DocumentToClient(VisibleDocumentRectangleF)));
            }
        }

        /// <summary>
        /// Returns a rectangle in client coordinates that denotes the space that the document
        /// may take up. This is essentially the ClientRectangle converted to screen coordinates
        /// and then with the rulers and scrollbars subtracted out.
        /// </summary>
        public Rectangle VisibleViewRectangle
        {
            get
            {
                Rectangle clientRect = this._panel.ClientRectangle;
                Rectangle screenRect = this._panel.RectangleToScreen(clientRect);
                Rectangle ourClientRect = RectangleToClient(screenRect);
                return ourClientRect;
            }
        }

        #endregion

        #region Document

        public event EventHandler<EventArgs<Document>> DocumentChanging;

        protected virtual void OnDocumentChanging(Document newDocument)
        {
            if (DocumentChanging != null)
            {
                DocumentChanging(this, new EventArgs<Document>(newDocument));
            }
        }

        public event EventHandler DocumentChanged;

        protected virtual void OnDocumentChanged()
        {
            if (DocumentChanged != null)
            {
                DocumentChanged(this, EventArgs.Empty);
            }
        }

        [Browsable(false)]
        public Document Document
        {
            get { return _document; }

            set
            {
                if (InvokeRequired)
                {
                    this.Invoke(new Procedure<Document>(DocumentSetImpl), new object[] { value });
                }
                else
                {
                    DocumentSetImpl(value);
                }
            }
        }

        private void DocumentSetImpl(Document value)
        {
            PointF dspf = DocumentScrollPositionF;

            OnDocumentChanging(value);
            SuspendRefresh();

            try
            {
                if (this._document != null)
                {
                    this._document.Invalidated -= Document_Invalidated;
                    this._document.Metadata.Changed -= DocumentMetaDataChangedHandler;
                }

                this._document = value;

                if (_document != null)
                {
                    if (this._compositionSurface != null &&
                        this._compositionSurface.Size != _document.Size)
                    {
                        this._compositionSurface.Dispose();
                        this._compositionSurface = null;
                    }

                    if (this._compositionSurface == null)
                    {
                        this._compositionSurface = new Surface(Document.Size);
                    }

                    this._compositionSurface.Clear(ColorBgra.White);

                    if (this._surfaceBox.Surface != this._compositionSurface)
                    {
                        //                        this._surfaceBox.Surface = this._compositionSurface;
                        this._surfaceBox.Surface = ((BitmapLayer)_document.Layers[0]).Surface;
                    }

                    if (this.ScaleFactor != this._surfaceBox.ScaleFactor)
                    {
                        this.ScaleFactor = this._surfaceBox.ScaleFactor;
                    }

                    this._document.Invalidated += Document_Invalidated;
                    this._document.Metadata.Changed += DocumentMetaDataChangedHandler;
                }

                Invalidate(true);
                DocumentMetaDataChangedHandler(this, EventArgs.Empty);
                this.OnResize(EventArgs.Empty);
                OnDocumentChanged();
            }

            finally
            {
                ResumeRefresh();
            }

            DocumentScrollPositionF = dspf;
        }

        private void DocumentMetaDataChangedHandler(object sender, EventArgs e)
        {
            if (this._document != null)
            {
                this._leftRuler.Dpu = 1 / _document.PixelToPhysicalY(1, this._leftRuler.MeasurementUnit);
                this._topRuler.Dpu = 1 / _document.PixelToPhysicalY(1, this._topRuler.MeasurementUnit);
            }
        }

        #endregion

        #region Focus

        private void Panel_LostFocus(object sender, EventArgs e)
        {
            this._raiseFirstInputAfterGotFocus = false;
        }

        private void Panel_GotFocus(object sender, EventArgs e)
        {
            this._raiseFirstInputAfterGotFocus = true;
        }

        public event EventHandler FirstInputAfterGotFocus;
        protected virtual void OnFirstInputAfterGotFocus()
        {
            if (FirstInputAfterGotFocus != null)
            {
                FirstInputAfterGotFocus(this, EventArgs.Empty);
            }
        }

        public new void Focus()
        {
            this._panel.Focus();
        }

        private void CheckForFirstInputAfterGotFocus()
        {
            if (this._raiseFirstInputAfterGotFocus)
            {
                this._raiseFirstInputAfterGotFocus = false;
                OnFirstInputAfterGotFocus();
            }
        }

        [Browsable(false)]
        public override bool Focused
        {
            get
            {
                return base.Focused || _panel.Focused || _surfaceBox.Focused || _controlShadow.Focused || _leftRuler.Focused || _topRuler.Focused;
            }
        }

        #endregion

        #region Ruler

        /// <summary>
        /// Used to enable or disable the rulers.
        /// </summary>
        public bool RulersEnabled
        {
            get { return _rulersEnabled; }

            set
            {
                if (_rulersEnabled != value)
                {
                    _rulersEnabled = value;

                    if (_topRuler != null)
                    {
                        _topRuler.Enabled = value;
                        _topRuler.Visible = value;
                    }

                    if (_leftRuler != null)
                    {
                        _leftRuler.Enabled = value;
                        _leftRuler.Visible = value;
                    }

                    this.OnResize(EventArgs.Empty);
                    OnRulersEnabledChanged();
                }
            }
        }

        private void UpdateRulerOffsets()
        {
            // TODO: cleanse magic numbers
            this._topRuler.Offset = ScaleFactor.UnscaleScalar(UserInterface.ScaleWidth(-16.0f) - _surfaceBox.Location.X);
            this._topRuler.Update();
            this._leftRuler.Offset = ScaleFactor.UnscaleScalar(0.0f - _surfaceBox.Location.Y);
            this._leftRuler.Update();
        }


        public event EventHandler RulersEnabledChanged;

        protected void OnRulersEnabledChanged()
        {
            if (RulersEnabledChanged != null)
            {
                RulersEnabledChanged(this, EventArgs.Empty);
            }
        }

        #endregion

        #region Scroll

        public bool ScrollBarsVisible
        {
            get
            {
                return this.HScroll || this.VScroll;
            }
        }

        public bool PanelAutoScroll
        {
            get { return _panel.AutoScroll; }

            set
            {
                if (_panel.AutoScroll != value)
                {
                    _panel.AutoScroll = value;
                }
            }
        }

        private void Panel_Scroll(object sender, ScrollEventArgs e)
        {
            OnScroll(e);
            UpdateRulerOffsets();
        }

        public void RecenterView(PointF newCenter)
        {
            RectangleF visibleRect = VisibleDocumentRectangleF;

            var cornerPt = new PointF(
                newCenter.X - (visibleRect.Width / 2),
                newCenter.Y - (visibleRect.Height / 2));

            this.DocumentScrollPositionF = cornerPt;
        }

        /// <summary>
        /// Get or set upper left of scroll location in document coordinates.
        /// </summary>
        [Browsable(false)]
        public PointF DocumentScrollPositionF
        {
            get
            {
                if (this._panel == null || this._surfaceBox == null)
                {
                    return PointF.Empty;
                }
                else
                {
                    return VisibleDocumentRectangleF.Location;
                }
            }

            set
            {
                if (_panel == null)
                {
                    return;
                }

                PointF sbClientF = this._surfaceBox.SurfaceToClient(value);
                Point sbClient = Point.Round(sbClientF);

                if (this._panel.AutoScrollPosition != new Point(-sbClient.X, -sbClient.Y))
                {
                    this._panel.AutoScrollPosition = sbClient;
                    UpdateRulerOffsets();
                    this._topRuler.Invalidate();
                    this._leftRuler.Invalidate();
                }
            }
        }

        [Browsable(false)]
        public PointF DocumentCenterPointF
        {
            get
            {
                RectangleF vsb = VisibleDocumentRectangleF;
                PointF centerPt = new PointF((vsb.Left + vsb.Right) / 2, (vsb.Top + vsb.Bottom) / 2);
                return centerPt;
            }

            set
            {
                RectangleF vsb = VisibleDocumentRectangleF;
                PointF newCornerPt = new PointF(value.X - (vsb.Width / 2), value.Y - (vsb.Height / 2));
                this.DocumentScrollPositionF = newCornerPt;
            }
        }

        #endregion

        #region Mouse

        public event EventHandler DocumentMouseEnter;

        protected virtual void OnDocumentMouseEnter(EventArgs e)
        {
            if (DocumentMouseEnter != null)
            {
                DocumentMouseEnter(this, e);
            }
        }

        public event EventHandler DocumentMouseLeave;

        protected virtual void OnDocumentMouseLeave(EventArgs e)
        {
            if (DocumentMouseLeave != null)
            {
                DocumentMouseLeave(this, e);
            }
        }

        public event MouseEventHandler DocumentMouseMove;

        protected virtual void OnDocumentMouseMove(MouseEventArgs e)
        {
            if (DocumentMouseMove != null)
            {
                DocumentMouseMove(this, e);
            }
        }

        public void PerformDocumentMouseMove(MouseEventArgs e)
        {
            OnDocumentMouseMove(e);
        }

        public event MouseEventHandler DocumentMouseUp;

        protected virtual void OnDocumentMouseUp(MouseEventArgs e)
        {
            CheckForFirstInputAfterGotFocus();

            if (DocumentMouseUp != null)
            {
                DocumentMouseUp(this, e);
            }
        }

        public void PerformDocumentMouseUp(MouseEventArgs e)
        {
            OnDocumentMouseUp(e);
        }

        public event MouseEventHandler DocumentMouseDown;

        protected virtual void OnDocumentMouseDown(MouseEventArgs e)
        {
            CheckForFirstInputAfterGotFocus();

            if (DocumentMouseDown != null)
            {
                DocumentMouseDown(this, e);
            }
        }

        public void PerformDocumentMouseDown(MouseEventArgs e)
        {
            OnDocumentMouseDown(e);
        }

        public event EventHandler DocumentClick;

        protected void OnDocumentClick()
        {
            CheckForFirstInputAfterGotFocus();

            if (DocumentClick != null)
            {
                DocumentClick(this, EventArgs.Empty);
            }
        }

        public void PerformMouseWheel(Control sender, MouseEventArgs e)
        {
            HandleMouseWheel(sender, e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            HandleMouseWheel(this, e);
            base.OnMouseWheel(e);
        }

        protected virtual void HandleMouseWheel(Control sender, MouseEventArgs e)
        {
            // scroll by e.Delta pixels, in screen coordinates
            double docDelta = (double)e.Delta / this.ScaleFactor.Ratio;
            double oldX = this.DocumentScrollPositionF.X;
            double oldY = this.DocumentScrollPositionF.Y;
            double newX;
            double newY;

            if (ModifierKeys == Keys.Shift)
            {
                // scroll horizontally
                newX = this.DocumentScrollPositionF.X - docDelta;
                newY = this.DocumentScrollPositionF.Y;
            }
            else if (ModifierKeys == Keys.None)
            {
                // scroll vertically
                newX = this.DocumentScrollPositionF.X;
                newY = this.DocumentScrollPositionF.Y - docDelta;
            }
            else
            {
                // no change
                newX = this.DocumentScrollPositionF.X;
                newY = this.DocumentScrollPositionF.Y;
            }

            if (newX != oldX || newY != oldY)
            {
                this.DocumentScrollPositionF = new PointF((float)newX, (float)newY);
                UpdateRulerOffsets();
            }
        }

        public override bool IsMouseCaptured()
        {
            return this.Capture || _panel.Capture || _surfaceBox.Capture || _controlShadow.Capture || _leftRuler.Capture || _topRuler.Capture;
        }

        #endregion

        #region Keyword

        public event KeyPressEventHandler DocumentKeyPress;

        protected void OnDocumentKeyPress(KeyPressEventArgs e)
        {
            CheckForFirstInputAfterGotFocus();

            if (DocumentKeyPress != null)
            {
                DocumentKeyPress(this, e);
            }
        }

        private void Panel_KeyPress(object sender, KeyPressEventArgs e)
        {
            OnDocumentKeyPress(e);
        }

        public event KeyEventHandler DocumentKeyDown;

        protected void OnDocumentKeyDown(KeyEventArgs e)
        {
            CheckForFirstInputAfterGotFocus();

            if (DocumentKeyDown != null)
            {
                DocumentKeyDown(this, e);
            }
        }

        private void Panel_KeyDown(object sender, KeyEventArgs e)
        {
            CheckForFirstInputAfterGotFocus();

            OnDocumentKeyDown(e);

            if (!e.Handled)
            {
                PointF oldPt = this.DocumentScrollPositionF;
                PointF newPt = oldPt;
                RectangleF vdr = VisibleDocumentRectangleF;

                switch (e.KeyData)
                {
                    case Keys.Next:
                        newPt.Y += vdr.Height;
                        break;

                    case (Keys.Next | Keys.Shift):
                        newPt.X += vdr.Width;
                        break;

                    case Keys.Prior:
                        newPt.Y -= vdr.Height;
                        break;

                    case (Keys.Prior | Keys.Shift):
                        newPt.X -= vdr.Width;
                        break;

                    case Keys.Home:
                        if (oldPt.X == 0)
                        {
                            newPt.Y = 0;
                        }
                        else
                        {
                            newPt.X = 0;
                        }
                        break;

                    case Keys.End:
                        if (vdr.Right < this._document.Width - 1)
                        {
                            newPt.X = this._document.Width;
                        }
                        else
                        {
                            newPt.Y = this._document.Height;
                        }
                        break;

                    default:
                        break;
                }

                if (newPt != oldPt)
                {
                    DocumentScrollPositionF = newPt;
                    e.Handled = true;
                }
            }
        }

        public event KeyEventHandler DocumentKeyUp;

        protected void OnDocumentKeyUp(KeyEventArgs e)
        {
            CheckForFirstInputAfterGotFocus();

            if (DocumentKeyUp != null)
            {
                DocumentKeyUp(this, e);
            }
        }

        private void Panel_KeyUp(object sender, KeyEventArgs e)
        {
            OnDocumentKeyUp(e);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            Keys keyCode = keyData & Keys.KeyCode;

            if (Utility.IsArrowKey(keyData) ||
                keyCode == Keys.Delete ||
                keyCode == Keys.Tab)
            {
                KeyEventArgs kea = new KeyEventArgs(keyData);

                // We only intercept WM_KEYDOWN because WM_KEYUP is not sent!
                switch (msg.Msg)
                {
                    case 0x100: //NativeMethods.WmConstants.WM_KEYDOWN:
                        if (this.ContainsFocus)
                        {
                            OnDocumentKeyDown(kea);
                            //OnDocumentKeyUp(kea);

                            if (Utility.IsArrowKey(keyData))
                            {
                                kea.Handled = true;
                            }
                        }

                        if (kea.Handled)
                        {
                            return true;
                        }

                        break;

                    /*
            case 0x101: //NativeMethods.WmConstants.WM_KEYUP:
                if (this.ContainsFocus)
                {
                    OnDocumentKeyUp(kea);
                }

                return kea.Handled;
                */
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        #endregion

        #region Invalidate and redraw

        public void InvalidateSurface(Rectangle rect)
        {
            this._surfaceBox.Invalidate(rect);
            InvalidateControlShadow(rect);
        }

        public void InvalidateSurface()
        {
            _surfaceBox.Invalidate();
            _controlShadow.Invalidate();
        }

        private void InvalidateControlShadowNoClipping(Rectangle rect)
        {
            if (rect.Width > 0 && rect.Height > 0)
            {
                Rectangle csRect = SurfaceBoxToControlShadow(rect);
                this._controlShadow.Invalidate(csRect);
            }
        }

        private void InvalidateControlShadow(Rectangle surfaceBoxRect)
        {
            if (this._document == null)
            {
                return;
            }

            Rectangle maxRect = SurfaceBoxRenderer.MaxBounds;
            Size surfaceBoxSize = this._surfaceBox.Size;

            Rectangle leftRect = Rectangle.FromLTRB(maxRect.Left, 0, 0, surfaceBoxSize.Height);
            Rectangle topRect = Rectangle.FromLTRB(maxRect.Left, maxRect.Top, maxRect.Right, 0);
            Rectangle rightRect = Rectangle.FromLTRB(surfaceBoxSize.Width, 0, maxRect.Right, surfaceBoxSize.Height);
            Rectangle bottomRect = Rectangle.FromLTRB(maxRect.Left, surfaceBoxSize.Height, maxRect.Right, maxRect.Bottom);

            leftRect.Intersect(surfaceBoxRect);
            topRect.Intersect(surfaceBoxRect);
            rightRect.Intersect(surfaceBoxRect);
            bottomRect.Intersect(surfaceBoxRect);

            InvalidateControlShadowNoClipping(leftRect);
            InvalidateControlShadowNoClipping(topRect);
            InvalidateControlShadowNoClipping(rightRect);
            InvalidateControlShadowNoClipping(bottomRect);
        }

        private void Document_Invalidated(object sender, InvalidateEventArgs e)
        {
            // Note: We don't need to convert this rectangle to controlShadow coordinates and invalidate it
            // because, by definition, any invalidation on the document should be within the document's
            // bounds and thus within the surfaceBox's bounds and thus outside the controlShadow's clipping
            // region.

            if (this.ScaleFactor == ScaleFactor.OneToOne)
            {
                this._surfaceBox.Invalidate(e.InvalidRect);
            }
            else
            {
                Rectangle inflatedInvalidRect = Rectangle.Inflate(e.InvalidRect, 1, 1);
                Rectangle clientRect = _surfaceBox.SurfaceToClient(inflatedInvalidRect);
                Rectangle inflatedClientRect = Rectangle.Inflate(clientRect, 1, 1);
                this._surfaceBox.Invalidate(inflatedClientRect);
            }
        }

        private void ControlShadow_Paint(object sender, PaintEventArgs e)
        {
            SurfaceBoxRenderer[][] renderers = this._surfaceBox.RendererList.Renderers;

            Rectangle csScreenRect = this.RectangleToScreen(this._controlShadow.Bounds);
            Rectangle sbScreenRect = this.RectangleToScreen(this._surfaceBox.Bounds);
            Point offset = new Point(sbScreenRect.X - csScreenRect.X, sbScreenRect.Y - csScreenRect.Y);

            foreach (SurfaceBoxRenderer[] renderList in renderers)
            {
                foreach (SurfaceBoxRenderer renderer in renderList)
                {
                    if (renderer.Visible)
                    {
                        var sbgr = renderer as SurfaceBoxGraphicsRenderer;

                        if (sbgr != null)
                        {
                            Matrix oldMatrix = e.Graphics.Transform;
                            sbgr.RenderToGraphics(e.Graphics, new Point(-offset.X, -offset.Y));
                            e.Graphics.Transform = oldMatrix;
                        }
                    }
                }
            }
        }

        private void Renderers_Invalidated(object sender, InvalidateEventArgs e)
        {
            if (this._document != null)
            {
                RectangleF rectF = this._surfaceBox.RendererList.SourceToDestination(e.InvalidRect);
                Rectangle rect = Utility.RoundRectangle(rectF);
                InvalidateControlShadow(rect);
            }
        }

        #endregion

        #region Refresh

        public void SuspendRefresh()
        {
            ++this._refreshSuspended;

            this._surfaceBox.Visible
                = this._controlShadow.Visible = (_refreshSuspended <= 0);
        }

        public void ResumeRefresh()
        {
            --this._refreshSuspended;

            this._surfaceBox.Visible
                = this._controlShadow.Visible = (_refreshSuspended <= 0);
        }

        #endregion

        #region Layout

        protected override void OnLayout(LayoutEventArgs e)
        {
            DoLayout();
            base.OnLayout(e);
        }

        private void DoLayout()
        {
            // Ensure that the document is centered.
            if (_panel.ClientRectangle != new Rectangle(0, 0, 0, 0))
            {
                // If the client area is bigger than the area used to display the image, center it
                int newX = _panel.AutoScrollPosition.X;
                int newY = _panel.AutoScrollPosition.Y;

                if (_panel.ClientRectangle.Width > _surfaceBox.Width)
                {
                    newX = _panel.AutoScrollPosition.X + ((_panel.ClientRectangle.Width - _surfaceBox.Width) / 2);
                }

                if (_panel.ClientRectangle.Height > _surfaceBox.Height)
                {
                    newY = _panel.AutoScrollPosition.Y + ((_panel.ClientRectangle.Height - _surfaceBox.Height) / 2);
                }

                var newPoint = new Point(newX, newY);

                if (!_surfaceBox.Location.Equals(newPoint))
                {
                    _surfaceBox.Location = newPoint;
                }
            }

            this.UpdateRulerOffsets();
        }

        protected override void OnResize(EventArgs e)
        {
            // enable or disable timer: no sense drawing selection if we're minimized
            Form parentForm = ParentForm;

            if (parentForm != null)
            {
                if (parentForm.WindowState != this._oldWindowState)
                {
                    PerformLayout();
                }

                this._oldWindowState = parentForm.WindowState;
            }

            base.OnResize(e);
            DoLayout();
        }

        #endregion

        #region Composition

        public event EventHandler CompositionUpdated;
        private void OnCompositionUpdated()
        {
            if (CompositionUpdated != null)
            {
                CompositionUpdated(this, EventArgs.Empty);
            }
        }

        protected void RenderCompositionTo(Surface dst, bool highQuality, bool forceUpToDate)
        {
            if (forceUpToDate)
            {
                UpdateComposition(false);
            }

            if (dst.Width == this._compositionSurface.Width &&
                dst.Height == this._compositionSurface.Height)
            {
                dst.ClearWithCheckboardPattern();
                new UserBlendOps.NormalBlendOp().Apply(dst, this._compositionSurface);
            }
            else if (highQuality)
            {
                Surface thumb = new Surface(dst.Size);
                thumb.SuperSamplingFitSurface(this._compositionSurface);

                dst.ClearWithCheckboardPattern();

                new UserBlendOps.NormalBlendOp().Apply(dst, thumb);

                thumb.Dispose();
            }
            else
            {
                this._surfaceBox.RenderTo(dst);
            }
        }

        private int _withheldCompositionUpdatedCount;
        protected void UpdateComposition(bool raiseEvent)
        {
            lock (this)
            {
                using (var ra = new RenderArgs(this._compositionSurface))
                {
                    bool result = this._document.Update(ra);

                    if (raiseEvent && (result || this._withheldCompositionUpdatedCount > 0))
                    {
                        OnCompositionUpdated();

                        if (!result && this._withheldCompositionUpdatedCount > 0)
                        {
                            --this._withheldCompositionUpdatedCount;
                        }
                    }
                    else if (!raiseEvent && result)
                    {
                        // If they want to not raise the event, we must keep track so that
                        // the next time UpdateComposition() is called we still raise this
                        // event even if Update() returned false (which indicates there
                        // was nothing to update)
                        ++this._withheldCompositionUpdatedCount;
                    }

                }
            }
        }

        #endregion

        public void SetHighlightRectangle(RectangleF rectF)
        {
            if (rectF.Width == 0 || rectF.Height == 0)
            {
                this._leftRuler.HighlightEnabled = false;
                this._topRuler.HighlightEnabled = false;
            }
            else
            {
                if (this._topRuler != null)
                {
                    this._topRuler.HighlightEnabled = true;
                    this._topRuler.HighlightStart = rectF.Left;
                    this._topRuler.HighlightLength = rectF.Width;
                }

                if (this._leftRuler != null)
                {
                    this._leftRuler.HighlightEnabled = true;
                    this._leftRuler.HighlightStart = rectF.Top;
                    this._leftRuler.HighlightLength = rectF.Height;
                }
            }
        }
    }
}
