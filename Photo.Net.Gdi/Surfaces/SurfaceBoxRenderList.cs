using System;
using System.Drawing;
using System.Windows.Forms;
using Photo.Net.Core;

namespace Photo.Net.Gdi.Surfaces
{
    public sealed class SurfaceBoxRenderList
    {
        private SurfaceBoxRenderer[] _list;
        private SurfaceBoxRenderer[] _topList;
        private Size _sourceSize;
        private Size _destinationSize;
        private ScaleFactor _scaleFactor; // ratio is dst:src
        private readonly object _lockObject = new object();

        public object SyncRoot
        {
            get
            {
                return this._lockObject;
            }
        }

        public ScaleFactor ScaleFactor
        {
            get
            {
                return this._scaleFactor;
            }
        }

        public SurfaceBoxRenderer[][] Renderers
        {
            get
            {
                return new[] { _list, _topList };
            }
        }

        private void ComputeScaleFactor()
        {
            _scaleFactor = new ScaleFactor(this.DestinationSize.Width, this.SourceSize.Width);
        }

        public Point SourceToDestination(Point pt)
        {
            return this._scaleFactor.ScalePoint(pt);
        }

        public RectangleF SourceToDestination(Rectangle rect)
        {
            return this._scaleFactor.ScaleRectangle((RectangleF)rect);
        }

        public Point DestinationToSource(Point pt)
        {
            return this._scaleFactor.UnscalePoint(pt);
        }

        public void Add(SurfaceBoxRenderer addMe, bool alwaysOnTop)
        {
            SurfaceBoxRenderer[] startList = alwaysOnTop ? this._topList : this._list;
            var listPlusOne = new SurfaceBoxRenderer[startList.Length + 1];

            for (int i = 0; i < startList.Length; ++i)
            {
                listPlusOne[i] = startList[i];
            }

            listPlusOne[listPlusOne.Length - 1] = addMe;

            if (alwaysOnTop)
            {
                this._topList = listPlusOne;
            }
            else
            {
                this._list = listPlusOne;
            }

            Invalidate();
        }

        public void Remove(SurfaceBoxRenderer removeMe)
        {
            if (this._list.Length == 0 && this._topList.Length == 0)
            {
                throw new InvalidOperationException("zero items left, can't remove anything");
            }
            else
            {
                bool found = false;

                if (this._list.Length > 0)
                {
                    var listSubOne = new SurfaceBoxRenderer[this._list.Length - 1];
                    bool foundHere = false;
                    int dstIndex = 0;

                    for (int i = 0; i < this._list.Length; ++i)
                    {
                        if (this._list[i] == removeMe)
                        {
                            if (foundHere)
                            {
                                throw new ArgumentException("removeMe appeared multiple times in the list");
                            }
                            else
                            {
                                foundHere = true;
                            }
                        }
                        else
                        {
                            if (dstIndex == this._list.Length - 1)
                            {
                                // was not found
                            }
                            else
                            {
                                listSubOne[dstIndex] = this._list[i];
                                ++dstIndex;
                            }
                        }
                    }

                    if (foundHere)
                    {
                        this._list = listSubOne;
                        found = true;
                    }
                }

                if (this._topList.Length > 0)
                {
                    var topListSubOne = new SurfaceBoxRenderer[this._topList.Length - 1];
                    int topDstIndex = 0;
                    bool foundHere = false;

                    for (int i = 0; i < this._topList.Length; ++i)
                    {
                        if (this._topList[i] == removeMe)
                        {
                            if (found || foundHere)
                            {
                                throw new ArgumentException("removeMe appeared multiple times in the list");
                            }
                            else
                            {
                                foundHere = true;
                            }
                        }
                        else
                        {
                            if (topDstIndex == this._topList.Length - 1)
                            {
                                // was not found
                            }
                            else
                            {
                                topListSubOne[topDstIndex] = this._topList[i];
                                ++topDstIndex;
                            }
                        }
                    }

                    if (foundHere)
                    {
                        this._topList = topListSubOne;
                        found = true;
                    }
                }

                if (!found)
                {
                    throw new ArgumentException("removeMe was not found", "removeMe");
                }

                Invalidate();
            }
        }

        private void OnDestinationSizeChanged()
        {
            InvalidateLookups();

            if (this._destinationSize.Width != 0 && this._sourceSize.Width != 0)
            {
                ComputeScaleFactor();

                for (int i = 0; i < this._list.Length; ++i)
                {
                    this._list[i].OnDestinationSizeChanged();
                }

                for (int i = 0; i < this._topList.Length; ++i)
                {
                    this._topList[i].OnDestinationSizeChanged();
                }
            }
        }

        public Size DestinationSize
        {
            get
            {
                return this._destinationSize;
            }

            set
            {
                if (this._destinationSize != value)
                {
                    this._destinationSize = value;
                    OnDestinationSizeChanged();
                }
            }
        }

        private void OnSourceSizeChanged()
        {
            InvalidateLookups();

            if (this._destinationSize.Width != 0 && this._sourceSize.Width != 0)
            {
                ComputeScaleFactor();

                for (int i = 0; i < this._list.Length; ++i)
                {
                    this._list[i].OnSourceSizeChanged();
                }

                for (int i = 0; i < this._topList.Length; ++i)
                {
                    this._topList[i].OnSourceSizeChanged();
                }
            }
        }

        public Size SourceSize
        {
            get
            {
                return this._sourceSize;
            }

            set
            {
                if (this._sourceSize != value)
                {
                    this._sourceSize = value;
                    OnSourceSizeChanged();
                }
            }
        }

        public int[] Dst2SrcLookupX
        {
            get
            {
                lock (this.SyncRoot)
                {
                    CreateD2SLookupX();
                }

                return this._d2SLookupX;
            }
        }

        private int[] _d2SLookupX; // maps from destination->source coordinates
        private void CreateD2SLookupX()
        {
            if (this._d2SLookupX == null || this._d2SLookupX.Length != this.DestinationSize.Width + 1)
            {
                this._d2SLookupX = new int[this.DestinationSize.Width + 1];

                for (int x = 0; x < _d2SLookupX.Length; ++x)
                {
                    Point pt = new Point(x, 0);
                    Point surfacePt = this.DestinationToSource(pt);

                    // Sometimes the scale factor is slightly different on one axis than
                    // on another, simply due to accuracy. So we have to clamp this value to
                    // be within bounds.
                    _d2SLookupX[x] = Utility.Clamp(surfacePt.X, 0, this.SourceSize.Width - 1);
                }
            }
        }

        public int[] Dst2SrcLookupY
        {
            get
            {
                lock (this.SyncRoot)
                {
                    CreateD2SLookupY();
                }

                return this._d2SLookupY;
            }
        }

        private int[] _d2SLookupY; // maps from destination->source coordinates
        private void CreateD2SLookupY()
        {
            if (this._d2SLookupY == null || this._d2SLookupY.Length != this.DestinationSize.Height + 1)
            {
                this._d2SLookupY = new int[this.DestinationSize.Height + 1];

                for (int y = 0; y < _d2SLookupY.Length; ++y)
                {
                    Point pt = new Point(0, y);
                    Point surfacePt = this.DestinationToSource(pt);

                    // Sometimes the scale factor is slightly different on one axis than
                    // on another, simply due to accuracy. So we have to clamp this value to
                    // be within bounds.
                    _d2SLookupY[y] = Utility.Clamp(surfacePt.Y, 0, this.SourceSize.Height - 1);
                }
            }
        }

        public int[] Src2DstLookupX
        {
            get
            {
                lock (this.SyncRoot)
                {
                    CreateS2DLookupX();
                }

                return this._s2DLookupX;
            }
        }

        private int[] _s2DLookupX; // maps from source->destination coordinates
        private void CreateS2DLookupX()
        {
            if (this._s2DLookupX == null || this._s2DLookupX.Length != this.SourceSize.Width + 1)
            {
                this._s2DLookupX = new int[this.SourceSize.Width + 1];

                for (int x = 0; x < _s2DLookupX.Length; ++x)
                {
                    Point pt = new Point(x, 0);
                    Point clientPt = this.SourceToDestination(pt);

                    // Sometimes the scale factor is slightly different on one axis than
                    // on another, simply due to accuracy. So we have to clamp this value to
                    // be within bounds.
                    _s2DLookupX[x] = Utility.Clamp(clientPt.X, 0, this.DestinationSize.Width - 1);
                }
            }
        }

        public int[] Src2DstLookupY
        {
            get
            {
                lock (this.SyncRoot)
                {
                    CreateS2DLookupY();
                }

                return this._s2DLookupY;
            }
        }

        private int[] _s2DLookupY; // maps from source->destination coordinates
        private void CreateS2DLookupY()
        {
            if (this._s2DLookupY == null || this._s2DLookupY.Length != this.SourceSize.Height + 1)
            {
                this._s2DLookupY = new int[this.SourceSize.Height + 1];

                for (int y = 0; y < _s2DLookupY.Length; ++y)
                {
                    var pt = new Point(0, y);
                    Point clientPt = this.SourceToDestination(pt);

                    // Sometimes the scale factor is slightly different on one axis than
                    // on another, simply due to accuracy. So we have to clamp this value to
                    // be within bounds.
                    _s2DLookupY[y] = clientPt.Y.Clamp(0, this.DestinationSize.Height - 1);
                }
            }
        }

        public void InvalidateLookups()
        {
            this._s2DLookupX = null;
            this._s2DLookupY = null;
            this._d2SLookupX = null;
            this._d2SLookupY = null;
        }

        public void Render(Surface dst, Point offset)
        {
            foreach (SurfaceBoxRenderer sbr in this._list)
            {
                if (sbr.Visible)
                {
                    sbr.Render(dst, offset);
                }
            }

            foreach (SurfaceBoxRenderer sbr in this._topList)
            {
                if (sbr.Visible)
                {
                    sbr.Render(dst, offset);
                }
            }
        }

        public event InvalidateEventHandler Invalidated;

        public void Invalidate(Rectangle rect)
        {
            if (Invalidated != null)
            {
                Invalidated(this, new InvalidateEventArgs(rect));
            }
        }

        public void Invalidate()
        {
            Invalidate(SurfaceBoxRenderer.MaxBounds);
        }

        public SurfaceBoxRenderList(Size sourceSize, Size destinationSize)
        {
            this._list = new SurfaceBoxRenderer[0];
            this._topList = new SurfaceBoxRenderer[0];
            this._sourceSize = sourceSize;
            this._destinationSize = destinationSize;
        }
    }
}