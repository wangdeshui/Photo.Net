using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Photo.Net.Base;
using Photo.Net.Core;
using Photo.Net.Core.Area;
using Photo.Net.Core.BitVector;
using Photo.Net.Gdi.Enums;
using Photo.Net.Gdi.Renders;
using Photo.Net.Gdi.Surfaces;
using Photo.Net.Resource;
using Photo.Net.Tool.Core;
using Photo.Net.Tool.Core.Enums;
using Photo.Net.Tool.Documents;
using Photo.Net.Tool.Events;
using Photo.Net.Tool.Layer;

namespace Photo.Net.Tool
{
    /// <summary>
    /// Encapsulates the functionality for a tool that goes in the main window's toolbar
    /// and that affects the Document.
    /// A Tool should only emit a HistoryMemento when it actually modifies the canvas.
    /// So, for instance, if the user draws a line but that line doesn't fall within
    /// the canvas (like if the seleciton region excludes it), then since the user
    /// hasn't really done anything there should be no HistoryMemento emitted.
    /// </summary>
    /// <remarks>
    /// A bit about the eventing model:
    /// * Perform[Event]() methods are ALWAYS used to trigger the events. This can be called by
    ///   either instance methods or outside (client) callers.
    /// * [Event]() methods are called first by Perform[Event](). This gives the base Tool class a
    ///   first chance at handling the event. These methods are private and non-overridable.
    /// * On[Event]() methods are then called by [Event]() if necessary, and should be overrided 
    ///   as necessary by derived classes. Always call the base implementation unless the
    ///   documentation says otherwise. The base implementation gives the Tool a chance to provide
    ///   default, overridable behavior for an event.
    /// </remarks>
    public class BaseTool
        : IDisposable,
          IHotKeyTarget
    {
        //        public static readonly Type DefaultToolType = typeof(Tools.PaintBrushTool);

        private readonly ImageResource _toolBarImage;
        private Cursor _cursor;
        private readonly ToolInfo _toolInfo;
        private int _mouseDownCount; // incremented for every MouseDown, decremented for every MouseUp
        private int _mouseEnterCount;
        protected int IgnoreMouseMoveCount; // when >0, MouseMove is ignored and then this is decremented

        private Point _lastMouseXy;
        protected Cursor HandCursor;
        protected Cursor HandCursorMouseDown;
        protected Cursor HandCursorInvalid;
        private MoveNubRenderer _trackingNub; // when we are in pan-tracking mode, we draw this in the center of the screen

        private readonly DocumentWorkspace _documentWorkspace;

        private bool _active = false;
        protected bool AutoScroll = true;
        private readonly Hashtable _keysThatAreDown = new Hashtable();
        private MouseButtons _lastButton = MouseButtons.None;
        private Surface _scratchSurface;
        private GeometryRegion _saveRegion;


        protected Surface ScratchSurface
        {
            get
            {
                return _scratchSurface;
            }
        }

        protected Document Document
        {
            get
            {
                return DocumentWorkspace.Document;
            }
        }

        public DocumentWorkspace DocumentWorkspace
        {
            get
            {
                return this._documentWorkspace;
            }
        }

        public AppWorkspace AppWorkspace
        {
            get
            {
                return DocumentWorkspace.AppWorkspace;
            }
        }

        protected ImageLayer ActiveLayer
        {
            get
            {
                return DocumentWorkspace.ActiveLayer;
            }
        }

        protected ToolEnvironment ToolEnvironment
        {
            get
            {
                return this.DocumentWorkspace.AppWorkspace.ToolEnvironment;
            }
        }

        protected Selection Selection
        {
            get
            {
                return DocumentWorkspace.Selection;
            }
        }

        protected int ActiveLayerIndex
        {
            get
            {
                return DocumentWorkspace.ActiveLayerIndex;
            }

            set
            {
                DocumentWorkspace.ActiveLayerIndex = value;
            }
        }

        public void ClearSavedMemory()
        {
            this._savedTiles = null;
        }

        public void ClearSavedRegion()
        {
            if (this._saveRegion != null)
            {
                this._saveRegion.Dispose();
                this._saveRegion = null;
            }
        }

        public void RestoreRegion(GeometryRegion region)
        {
            if (region != null)
            {
                var activeLayer = (BitmapLayer)ActiveLayer;
                activeLayer.Surface.CopySurface(this.ScratchSurface, region);
                activeLayer.Invalidate(region);
            }
        }

        public void RestoreSavedRegion()
        {
            if (this._saveRegion != null)
            {
                var activeLayer = (BitmapLayer)ActiveLayer;
                activeLayer.Surface.CopySurface(this.ScratchSurface, this._saveRegion);
                activeLayer.Invalidate(this._saveRegion);
                this._saveRegion.Dispose();
                this._saveRegion = null;
            }
        }

        private const int SaveTileGranularity = 32;
        private BitVector2D _savedTiles;

        public void SaveRegion(GeometryRegion saveMeRegion, Rectangle saveMeBounds)
        {
            var activeLayer = (BitmapLayer)ActiveLayer;

            if (_savedTiles == null)
            {
                _savedTiles = new BitVector2D(
                    (activeLayer.Width + SaveTileGranularity - 1) / SaveTileGranularity,
                    (activeLayer.Height + SaveTileGranularity - 1) / SaveTileGranularity);

                _savedTiles.Clear(false);
            }

            Rectangle regionBounds;
            if (saveMeRegion == null)
            {
                regionBounds = saveMeBounds;
            }
            else
            {
                regionBounds = saveMeRegion.GetBoundsInt();
            }

            Rectangle bounds = Rectangle.Union(regionBounds, saveMeBounds);
            bounds.Intersect(activeLayer.Bounds);

            int leftTile = bounds.Left / SaveTileGranularity;
            int topTile = bounds.Top / SaveTileGranularity;
            int rightTile = (bounds.Right - 1) / SaveTileGranularity;
            int bottomTile = (bounds.Bottom - 1) / SaveTileGranularity;

            for (int tileY = topTile; tileY <= bottomTile; ++tileY)
            {
                Rectangle rowAccumBounds = Rectangle.Empty;

                for (int tileX = leftTile; tileX <= rightTile; ++tileX)
                {
                    if (!_savedTiles.Get(tileX, tileY))
                    {
                        Rectangle tileBounds = new Rectangle(tileX * SaveTileGranularity, tileY * SaveTileGranularity,
                            SaveTileGranularity, SaveTileGranularity);

                        tileBounds.Intersect(activeLayer.Bounds);

                        if (rowAccumBounds == Rectangle.Empty)
                        {
                            rowAccumBounds = tileBounds;
                        }
                        else
                        {
                            rowAccumBounds = Rectangle.Union(rowAccumBounds, tileBounds);
                        }

                        _savedTiles.Set(tileX, tileY, true);
                    }
                    else
                    {
                        if (rowAccumBounds != Rectangle.Empty)
                        {
                            using (Surface dst = ScratchSurface.CreateWindow(rowAccumBounds),
                                           src = activeLayer.Surface.CreateWindow(rowAccumBounds))
                            {
                                dst.CopySurface(src);
                            }

                            rowAccumBounds = Rectangle.Empty;
                        }
                    }
                }

                if (rowAccumBounds != Rectangle.Empty)
                {
                    using (Surface dst = ScratchSurface.CreateWindow(rowAccumBounds),
                                   src = activeLayer.Surface.CreateWindow(rowAccumBounds))
                    {
                        dst.CopySurface(src);
                    }

                    rowAccumBounds = Rectangle.Empty;
                }
            }

            if (this._saveRegion != null)
            {
                this._saveRegion.Dispose();
                this._saveRegion = null;
            }

            if (saveMeRegion != null)
            {
                this._saveRegion = saveMeRegion.Clone();
            }
        }

        private sealed class KeyTimeInfo
        {
            public DateTime KeyDownTime;
            public DateTime LastKeyPressPulse;

            public int Repeats { get; set; }

            public KeyTimeInfo()
            {
                Repeats = 0;
                KeyDownTime = DateTime.Now;
                LastKeyPressPulse = KeyDownTime;
            }
        }

        /// <summary>
        /// Tells you whether the tool is "active" or not. If the tool is not active
        /// it is not safe to call any other method besides PerformActivate. All
        /// properties are safe to get values from.
        /// </summary>
        public bool Active
        {
            get
            {
                return this._active;
            }
        }

        /// <summary>
        /// Returns true if the Tool has the input focus, or false if it does not.
        /// </summary>
        /// <remarks>
        /// This is used, for instanced, by the Text Tool so that it doesn't blink the
        /// cursor unless it's actually going to do something in response to your
        /// keyboard input!
        /// </remarks>
        public bool Focused
        {
            get
            {
                return DocumentWorkspace.Focused;
            }
        }

        public bool IsMouseDown
        {
            get
            {
                return this._mouseDownCount > 0;
            }
        }

        /// <summary>
        /// Gets a flag that determines whether the Tool is deactivated while the current
        /// layer is changing, and then reactivated afterwards.
        /// </summary>
        /// <remarks>
        /// This property is queried every time the ActiveLayer property of DocumentWorkspace
        /// is changed. If false is returned, then the tool is not deactivated during the
        /// layer change and must manually maintain coherency.
        /// </remarks>
        public virtual bool DeactivateOnLayerChange
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Tells you which keys are pressed
        /// </summary>
        public Keys ModifierKeys
        {
            get
            {
                return Control.ModifierKeys;
            }
        }

        /// <summary>
        /// Represents the Image that is displayed in the toolbar.
        /// </summary>
        public ImageResource Image
        {
            get
            {
                return this._toolBarImage;
            }
        }

        public event EventHandler CursorChanging;
        protected virtual void OnCursorChanging()
        {
            if (CursorChanging != null)
            {
                CursorChanging(this, EventArgs.Empty);
            }
        }

        public event EventHandler CursorChanged;
        protected virtual void OnCursorChanged()
        {
            if (CursorChanged != null)
            {
                CursorChanged(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// The Cursor that is displayed when this Tool is active and the
        /// mouse cursor is inside the document view.
        /// </summary>
        public Cursor Cursor
        {
            get
            {
                return this._cursor;
            }

            set
            {
                OnCursorChanging();
                this._cursor = value;
                OnCursorChanged();
                DocumentWorkspace.UpdateCursor();
            }
        }

        /// <summary>
        /// The name of the Tool. For instance, "Pencil". This name should *not* end in "Tool", e.g. "Pencil Tool"
        /// </summary>
        public string Name
        {
            get
            {
                return this._toolInfo.Name;
            }
        }

        /// <summary>
        /// A short description of how to use the tool.
        /// </summary>
        public string HelpText
        {
            get
            {
                return this._toolInfo.HelpText;
            }
        }

        public ToolInfo Info
        {
            get
            {
                return this._toolInfo;
            }
        }

        public ToolBarConfigItems ToolBarConfigItems
        {
            get
            {
                return this._toolInfo.ToolBarConfigItems;
            }
        }

        /// <summary>
        /// Specifies whether or not an inherited tool should take Ink commands
        /// </summary>
        protected virtual bool SupportsInk
        {
            get
            {
                return false;
            }
        }

        public char HotKey
        {
            get
            {
                return this._toolInfo.HotKey;
            }
        }

        public void PerformActivate()
        {
            Activate();
        }

        public void PerformDeactivate()
        {
            Deactivate();
        }

        private bool IsOverflow(MouseEventArgs e)
        {
            PointF clientPt = DocumentWorkspace.DocumentToClient(new PointF(e.X, e.Y));
            return clientPt.X < -16384 || clientPt.Y < -16384;
        }

        public bool IsMouseEntered
        {
            get
            {
                return this._mouseEnterCount > 0;
            }
        }

        public void PerformMouseEnter(object sender, EventArgs args)
        {
            MouseEnter();
        }

        private void MouseEnter()
        {
            ++this._mouseEnterCount;

            if (this._mouseEnterCount == 1)
            {
                OnMouseEnter();
            }
        }

        protected virtual void OnMouseEnter()
        {
        }

        public void PerformMouseLeave(object sender, EventArgs args)
        {
            MouseLeave();
        }

        private void MouseLeave()
        {
            if (this._mouseEnterCount == 1)
            {
                this._mouseEnterCount = 0;
                OnMouseLeave();
            }
            else
            {
                this._mouseEnterCount = Math.Max(0, this._mouseEnterCount - 1);
            }
        }

        protected virtual void OnMouseLeave()
        {
        }

        public void PerformMouseMove(object sender, MouseEventArgs e)
        {
            if (IsOverflow(e))
            {
                return;
            }

            if (e is StylusEventArgs)
            {
                if (this.SupportsInk)
                {
                    StylusMove(e as StylusEventArgs);
                }

                // if the tool does not claim ink support, discard
            }
            else
            {
                MouseMove(e);
            }
        }

        public void PerformMouseDown(object sender, MouseEventArgs e)
        {
            if (IsOverflow(e))
            {
                return;
            }

            if (e is StylusEventArgs)
            {
                if (this.SupportsInk)
                {
                    StylusDown(e as StylusEventArgs);
                }

                // if the tool does not claim ink support, discard
            }
            else
            {
                if (this.SupportsInk)
                {
                    DocumentWorkspace.Focus();
                }

                MouseDown(e);
            }
        }

        public void PerformMouseUp(object sender, MouseEventArgs e)
        {
            if (IsOverflow(e))
            {
                return;
            }

            if (e is StylusEventArgs)
            {
                if (this.SupportsInk)
                {
                    StylusUp(e as StylusEventArgs);
                }

                // if the tool does not claim ink support, discard
            }
            else
            {
                MouseUp(e);
            }
        }

        public void PerformKeyPress(object sender, KeyPressEventArgs e)
        {
            KeyPress(e);
        }

        public void PerformKeyPress(Keys key)
        {
            KeyPress(key);
        }

        public void PerformKeyUp(object sender, KeyEventArgs e)
        {
            KeyUp(e);
        }

        public void PerformKeyDown(object sender, KeyEventArgs e)
        {
            KeyDown(e);
        }

        public void PerformClick(object sender, EventArgs args)
        {
            Click();
        }

        public void PerformPulse()
        {
            Pulse();
        }

        public void PerformPaste(IDataObject data, out bool handled)
        {
            Paste(data, out handled);
        }

        public void PerformPasteQuery(IDataObject data, out bool canHandle)
        {
            PasteQuery(data, out canHandle);
        }

        private void Activate()
        {
            this._active = true;

            this.HandCursor = new Cursor(PdnResources.GetResourceStream("Cursors.PanToolCursor.cur"));
            this.HandCursorMouseDown = new Cursor(PdnResources.GetResourceStream("Cursors.PanToolCursorMouseDown.cur"));
            this.HandCursorInvalid = new Cursor(PdnResources.GetResourceStream("Cursors.PanToolCursorInvalid.cur"));
            this.HandCursor = Cursors.Hand;
            this.HandCursorMouseDown = Cursors.Arrow;
            this.HandCursorInvalid = Cursors.Arrow;

            this._mouseDownCount = 0;
            this._savedTiles = null;
            this._saveRegion = null;

            this._scratchSurface = DocumentWorkspace.BorrowScratchSurface();
            //
            //                        Selection.Changing += new EventHandler(SelectionChangingHandler);
            //                        Selection.Changed += new EventHandler(SelectionChangedHandler);
            //                        HistoryStack.ExecutingHistoryMemento += new ExecutingHistoryMementoEventHandler(ExecutingHistoryMemento);
            //                        HistoryStack.ExecutedHistoryMemento += new ExecutedHistoryMementoEventHandler(ExecutedHistoryMemento);
            //                        HistoryStack.FinishedStepGroup += new EventHandler(FinishedHistoryStepGroup);

            this._trackingNub = new MoveNubRenderer(this.RendererList);
            this._trackingNub.Visible = false;
            this._trackingNub.Size = new SizeF(10, 10);
            this._trackingNub.Shape = MoveNubShape.Compass;
            this.RendererList.Add(this._trackingNub, false);

            OnActivate();
        }

        protected virtual void OnFinishedHistoryStepGroup()
        {
        }

        /// <summary>
        /// This method is called when the tool is being activated; that is, when the
        /// user has chosen to use this tool by clicking on it on a toolbar.
        /// </summary>
        protected virtual void OnActivate()
        {
        }

        private void Deactivate()
        {
            this._active = false;

            //                        Selection.Changing -= new EventHandler(SelectionChangingHandler);
            //                        Selection.Changed -= new EventHandler(SelectionChangedHandler);
            //            
            //                        HistoryStack.ExecutingHistoryMemento -= new ExecutingHistoryMementoEventHandler(ExecutingHistoryMemento);
            //                        HistoryStack.ExecutedHistoryMemento -= new ExecutedHistoryMementoEventHandler(ExecutedHistoryMemento);
            //                        HistoryStack.FinishedStepGroup -= new EventHandler(FinishedHistoryStepGroup);

            OnDeactivate();

            this.RendererList.Remove(this._trackingNub);
            this._trackingNub.Dispose();
            this._trackingNub = null;

            DocumentWorkspace.ReturnScratchSurface(this._scratchSurface);
            this._scratchSurface = null;

            if (this._saveRegion != null)
            {
                this._saveRegion.Dispose();
                this._saveRegion = null;
            }

            this._savedTiles = null;

            if (this.HandCursor != null)
            {
                this.HandCursor.Dispose();
                this.HandCursor = null;
            }

            if (this.HandCursorMouseDown != null)
            {
                this.HandCursorMouseDown.Dispose();
                this.HandCursorMouseDown = null;
            }

            if (this.HandCursorInvalid != null)
            {
                this.HandCursorInvalid.Dispose();
                this.HandCursorInvalid = null;
            }
        }

        /// <summary>
        /// This method is called when the tool is being deactivated; that is, when the
        /// user has chosen to use another tool by clicking on another tool on a
        /// toolbar.
        /// </summary>
        protected virtual void OnDeactivate()
        {
            UnBindEvent();
        }

        private void StylusDown(StylusEventArgs e)
        {
        }

        protected virtual void OnStylusDown(StylusEventArgs e)
        {
        }

        private void StylusMove(StylusEventArgs e)
        {
        }

        protected virtual void OnStylusMove(StylusEventArgs e)
        {
            if (this._mouseDownCount > 0)
            {
                ScrollIfNecessary(new PointF(e.X, e.Y));
            }
        }

        private void StylusUp(StylusEventArgs e)
        {
            OnStylusUp(e);
        }

        protected virtual void OnStylusUp(StylusEventArgs e)
        {
        }

        private void MouseMove(MouseEventArgs e)
        {
            if (this.IgnoreMouseMoveCount > 0)
            {
                --this.IgnoreMouseMoveCount;
            }
            OnMouseMove(e);

            this._lastMouseXy = new Point(e.X, e.Y);
            this._lastButton = e.Button;
        }

        /// <summary>
        /// This method is called when the Tool is active and the mouse is moving within
        /// the document canvas area.
        /// </summary>
        /// <param name="e">Contains information about where the mouse cursor is, in document coordinates.</param>
        protected virtual void OnMouseMove(MouseEventArgs e)
        {
        }

        private void MouseDown(MouseEventArgs e)
        {
            ++this._mouseDownCount;

            OnMouseDown(e);

            this._lastMouseXy = new Point(e.X, e.Y);
        }

        /// <summary>
        /// This method is called when the Tool is active and a mouse button has been
        /// pressed within the document area.
        /// </summary>
        /// <param name="e">Contains information about where the mouse cursor is, in document coordinates, and which mouse buttons were pressed.</param>
        protected virtual void OnMouseDown(MouseEventArgs e)
        {
            this._lastButton = e.Button;
        }

        private void MouseUp(MouseEventArgs e)
        {
            --this._mouseDownCount;

            OnMouseUp(e);

            this._lastMouseXy = new Point(e.X, e.Y);
        }

        /// <summary>
        /// This method is called when the Tool is active and a mouse button has been
        /// released within the document area.
        /// </summary>
        /// <param name="e">Contains information about where the mouse cursor is, in document coordinates, and which mouse buttons were released.</param>
        protected virtual void OnMouseUp(MouseEventArgs e)
        {
            this._lastButton = e.Button;
        }

        private void Click()
        {
            OnClick();
        }

        /// <summary>
        /// This method is called when the Tool is active and a mouse button has been
        /// clicked within the document area. If you need more specific information,
        /// such as where the mouse was clicked and which button was used, respond to
        /// the MouseDown/MouseUp events.
        /// </summary>
        protected virtual void OnClick()
        {
        }

        private void KeyPress(KeyPressEventArgs e)
        {
            OnKeyPress(e);
        }

        // Return true if the key is handled, false if not.
        protected virtual bool OnWildShortcutKey(int ordinal)
        {
            return false;
        }

        /// <summary>
        /// This method is called when the tool is active and a keyboard key is pressed
        /// and released. If you respond to the keyboard key, set e.Handled to true.
        /// </summary>
        protected virtual void OnKeyPress(KeyPressEventArgs e)
        {
            if (!e.Handled && DocumentWorkspace.Focused)
            {



            }
        }

        private DateTime _lastKeyboardMove = DateTime.MinValue;
        private Keys _lastKey;
        private int _keyboardMoveSpeed = 1;
        private int _keyboardMoveRepeats = 0;

        private void KeyPress(Keys key)
        {
            OnKeyPress(key);
        }

        /// <summary>
        /// This method is called when the tool is active and a keyboard key is pressed
        /// and released that is not representable with a regular Unicode chararacter.
        /// An example would be the arrow keys.
        /// </summary>
        protected virtual void OnKeyPress(Keys key)
        {
            Point dir = Point.Empty;

            if (key != _lastKey)
            {
                _lastKeyboardMove = DateTime.MinValue;
            }

            _lastKey = key;

            switch (key)
            {
                case Keys.Left:
                    --dir.X;
                    break;

                case Keys.Right:
                    ++dir.X;
                    break;

                case Keys.Up:
                    --dir.Y;
                    break;

                case Keys.Down:
                    ++dir.Y;
                    break;
            }

            if (!dir.Equals(Point.Empty))
            {
                long span = DateTime.Now.Ticks - _lastKeyboardMove.Ticks;

                if ((span * 4) > TimeSpan.TicksPerSecond)
                {
                    _keyboardMoveRepeats = 0;
                    _keyboardMoveSpeed = 1;
                }
                else
                {
                    _keyboardMoveRepeats++;

                    if (_keyboardMoveRepeats > 15 && (_keyboardMoveRepeats % 4) == 0)
                    {
                        _keyboardMoveSpeed++;
                    }
                }

                _lastKeyboardMove = DateTime.Now;

                int offset = (int)(Math.Ceiling(DocumentWorkspace.ScaleFactor.Ratio) * (double)_keyboardMoveSpeed);
                Cursor.Position = new Point(Cursor.Position.X + offset * dir.X, Cursor.Position.Y + offset * dir.Y);

                Point location = DocumentWorkspace.PointToScreen(Point.Truncate(DocumentWorkspace.DocumentToClient(PointF.Empty)));

                PointF stylusLocF = new PointF((float)Cursor.Position.X - (float)location.X, (float)Cursor.Position.Y - (float)location.Y);
                Point stylusLoc = new Point(Cursor.Position.X - location.X, Cursor.Position.Y - location.Y);

                stylusLoc = DocumentWorkspace.ScaleFactor.UnscalePoint(stylusLoc);
                stylusLocF = DocumentWorkspace.ScaleFactor.UnscalePoint(stylusLocF);

                DocumentWorkspace.PerformDocumentMouseMove(new StylusEventArgs(_lastButton, 1, stylusLocF.X, stylusLocF.Y, 0, 1.0f));
                DocumentWorkspace.PerformDocumentMouseMove(new MouseEventArgs(_lastButton, 1, stylusLoc.X, stylusLoc.Y, 0));
            }
        }

        private bool CanPan()
        {
            Rectangle vis = Utility.RoundRectangle(DocumentWorkspace.VisibleDocumentRectangleF);
            vis.Intersect(Document.Bounds);

            if (vis == Document.Bounds)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private void KeyUp(KeyEventArgs e)
        {
            OnKeyUp(e);
        }

        /// <summary>
        /// This method is called when the tool is active and a keyboard key is pressed.
        /// If you respond to the keyboard key, set e.Handled to true.
        /// </summary>
        protected virtual void OnKeyUp(KeyEventArgs e)
        {
            _keysThatAreDown.Clear();
        }

        private void KeyDown(KeyEventArgs e)
        {
            OnKeyDown(e);
        }

        /// <summary>
        /// This method is called when the tool is active and a keyboard key is released
        /// Before responding, check that e.Handled is false, and if you then respond to 
        /// the keyboard key, set e.Handled to true.
        /// </summary>
        protected virtual void OnKeyDown(KeyEventArgs e)
        {
            if (!e.Handled)
            {
                if (!_keysThatAreDown.Contains(e.KeyData))
                {
                    _keysThatAreDown.Add(e.KeyData, new KeyTimeInfo());
                }

                // arrow keys are processed in another way
                // we get their KeyDown but no KeyUp, so they can not be handled
                // by our normal methods
                OnKeyPress(e.KeyData);

            }
        }

        /// <summary>
        /// This method is called when the Tool is active and the selection area is
        /// about to be changed.
        /// </summary>
        protected virtual void OnSelectionChanging()
        {
        }

        /// <summary>
        /// This method is called when the Tool is active and the selection area has
        /// been changed.
        /// </summary>
        protected virtual void OnSelectionChanged()
        {
        }

        private void PasteQuery(IDataObject data, out bool canHandle)
        {
            OnPasteQuery(data, out canHandle);
        }

        /// <summary>
        /// This method is called when the system is querying a tool as to whether
        /// it can handle a pasted object.
        /// </summary>
        /// <param name="data">
        /// The clipboard data that was pasted by the user that should be inspected.
        /// </param>
        /// <param name="canHandle">
        /// <b>true</b> if the data can be handled by the tool, <b>false</b> if not.
        /// </param>
        /// <remarks>
        /// If you do not set canHandle to <b>true</b> then the tool will not be
        /// able to respond to the Edit menu's Paste item.
        /// </remarks>
        protected virtual void OnPasteQuery(IDataObject data, out bool canHandle)
        {
            canHandle = false;
        }

        private void Paste(IDataObject data, out bool handled)
        {
            OnPaste(data, out handled);
        }

        /// <summary>
        /// This method is called when the user invokes a paste operation. Tools get
        /// the first chance to handle this data.
        /// </summary>
        /// <param name="data">
        /// The data that was pasted by the user.
        /// </param>
        /// <param name="handled">
        /// <b>true</b> if the data was handled and pasted, <b>false</b> if not.
        /// </param>
        /// <remarks>
        /// If you do not set handled to <b>true</b> the event will be passed to the 
        /// global paste handler.
        /// </remarks>
        protected virtual void OnPaste(IDataObject data, out bool handled)
        {
            handled = false;
        }

        private void Pulse()
        {
            OnPulse();
        }

        protected bool IsFormActive
        {
            get
            {
                return (object.ReferenceEquals(Form.ActiveForm, DocumentWorkspace.FindForm()));
            }
        }

        /// <summary>
        /// This method is called many times per second, called by the DocumentWorkspace.
        /// </summary>
        protected virtual void OnPulse()
        {
            if (this._lastButton == MouseButtons.Right)
            {
                Point position = this._lastMouseXy;
                RectangleF visibleRect = DocumentWorkspace.VisibleDocumentRectangleF;
                PointF visibleCenterPt = Utility.GetRectangleCenter(visibleRect);
                PointF delta = new PointF(position.X - visibleCenterPt.X, position.Y - visibleCenterPt.Y);
                PointF newScroll = DocumentWorkspace.DocumentScrollPositionF;

                this._trackingNub.Visible = true;

                if (delta.X != 0 || delta.Y != 0)
                {
                    newScroll.X += delta.X;
                    newScroll.Y += delta.Y;

                    ++this.IgnoreMouseMoveCount; // setting DocumentScrollPosition incurs a MouseMove event. ignore it prevents 'jittering' at non-integral zoom levels (like, say, 743%)
                    UserInterface.SuspendControlPainting(DocumentWorkspace);
                    DocumentWorkspace.DocumentScrollPositionF = newScroll;
                    this._trackingNub.Visible = true;
                    this._trackingNub.Location = Utility.GetRectangleCenter(DocumentWorkspace.VisibleDocumentRectangleF);
                    UserInterface.ResumeControlPainting(DocumentWorkspace);
                    DocumentWorkspace.Invalidate(true);
                    Update();
                }
            }
        }

        protected bool ScrollIfNecessary(PointF position)
        {
            if (!AutoScroll || !CanPan())
            {
                return false;
            }

            RectangleF visible = DocumentWorkspace.VisibleDocumentRectangleF;
            PointF lastScrollPosition = DocumentWorkspace.DocumentScrollPositionF;
            PointF delta = PointF.Empty;
            PointF zoomedPoint = PointF.Empty;

            zoomedPoint.X = Utility.Lerp((visible.Left + visible.Right) / 2.0f, position.X, 1.02f);
            zoomedPoint.Y = Utility.Lerp((visible.Top + visible.Bottom) / 2.0f, position.Y, 1.02f);

            if (zoomedPoint.X < visible.Left)
            {
                delta.X = zoomedPoint.X - visible.Left;
            }
            else if (zoomedPoint.X > visible.Right)
            {
                delta.X = zoomedPoint.X - visible.Right;
            }

            if (zoomedPoint.Y < visible.Top)
            {
                delta.Y = zoomedPoint.Y - visible.Top;
            }
            else if (zoomedPoint.Y > visible.Bottom)
            {
                delta.Y = zoomedPoint.Y - visible.Bottom;
            }

            if (!delta.IsEmpty)
            {
                PointF newScrollPosition = new PointF(lastScrollPosition.X + delta.X, lastScrollPosition.Y + delta.Y);
                DocumentWorkspace.DocumentScrollPositionF = newScrollPosition;
                Update();
                return true;
            }
            else
            {
                return false;
            }
        }

        protected void SetStatus(ImageResource statusIcon, string statusText)
        {
            //            if (statusIcon == null && statusText != null)
            //            {
            //                statusIcon = PdnResources.GetImageResource("Icons.MenuHelpHelpTopicsIcon.png");
            //            }

            //            DocumentWorkspace.SetStatus(statusText, statusIcon);
        }

        protected SurfaceBoxRenderList RendererList
        {
            get
            {
                return this.DocumentWorkspace.RendererList;
            }
        }

        protected void Update()
        {
            DocumentWorkspace.Update();
        }

        #region Instance

        public BaseTool(DocumentWorkspace documentWorkspace,
            ImageResource toolBarImage,
            string name,
            string helpText,
            char hotKey,
            bool skipIfActiveOnHotKey,
            ToolBarConfigItems toolBarConfigItems)
        {
            this._documentWorkspace = documentWorkspace;
            this._toolBarImage = toolBarImage;
            this._toolInfo = new ToolInfo(name, helpText, toolBarImage, hotKey, skipIfActiveOnHotKey, toolBarConfigItems,
                this.GetType());

            if (documentWorkspace == null) return;

            documentWorkspace.DocumentKeyDown += PerformKeyDown;
            documentWorkspace.DocumentKeyUp += PerformKeyUp;
            documentWorkspace.DocumentKeyPress += PerformKeyPress;

            documentWorkspace.DocumentMouseEnter += PerformMouseEnter;
            documentWorkspace.DocumentMouseLeave += PerformMouseLeave;

            documentWorkspace.DocumentMouseDown += PerformMouseDown;
            documentWorkspace.DocumentMouseUp += PerformMouseUp;
            documentWorkspace.DocumentClick += PerformClick;
            documentWorkspace.DocumentMouseMove += PerformMouseMove;

        }

        #endregion

        #region Dispose

        ~BaseTool()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this._saveRegion != null)
                {
                    this._saveRegion.Dispose();
                    this._saveRegion = null;
                }


                OnDisposed();
            }
        }

        public event EventHandler Disposed;

        private void OnDisposed()
        {
            if (Disposed != null)
            {
                Disposed(this, EventArgs.Empty);
            }
        }

        private void UnBindEvent()
        {
            if (_documentWorkspace != null)
            {
                _documentWorkspace.DocumentKeyDown -= PerformKeyDown;
                _documentWorkspace.DocumentKeyUp -= PerformKeyUp;
                _documentWorkspace.DocumentKeyPress -= PerformKeyPress;

                _documentWorkspace.DocumentMouseEnter -= PerformMouseEnter;
                _documentWorkspace.DocumentMouseLeave -= PerformMouseLeave;

                _documentWorkspace.DocumentMouseDown -= PerformMouseDown;
                _documentWorkspace.DocumentMouseUp -= PerformMouseUp;
                _documentWorkspace.DocumentClick -= PerformClick;
                _documentWorkspace.DocumentMouseMove -= PerformMouseMove;
            }

        }

        #endregion

        public Form AssociatedForm
        {
            get
            {
                return AppWorkspace.FindForm();
            }
        }
    }
}
