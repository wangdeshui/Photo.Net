using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using Photo.Net.Base;
using Photo.Net.Base.Delegate;

namespace Photo.Net.Tool.Snap
{
    public abstract class SnapObstacle : PropertyChange
    {
        public const int DefaultSnapProximity = 15;
        public const int DefaultSnapDistance = 3;

        protected Rectangle _bounds;
        private readonly SnapRegion _snapRegion;
        private readonly bool _stickyEdges;
        private readonly int _snapProximity;
        private readonly int _snapDistance;

        public string Name { get; private set; }

        public SnapRegion SnapRegion
        {
            get
            {
                return this._snapRegion;
            }
        }

        /// <summary>
        /// Gets whether or not this obstacle has "sticky" edges.
        /// </summary>
        public bool StickyEdges
        {
            get
            {
                return this._stickyEdges;
            }
        }

        /// <summary>
        /// Gets how close another obstacle must be to snap to this one, in pixels
        /// </summary>
        public int SnapProximity
        {
            get
            {
                return this._snapProximity;
            }
        }

        /// <summary>
        /// Gets how close another obstacle will be parked when it snaps to this one, in pixels.
        /// </summary>
        public int SnapDistance
        {
            get
            {
                return this._snapDistance;
            }
        }

        public bool Enabled { get; set; }

        public bool EnableSave { get; set; }

        #region Bound

        /// <summary>
        /// Gets the bounds of this snap obstacle, defined in coordinates relative to its container.
        /// </summary>
        public Rectangle Bounds
        {
            get { return this._bounds; }
            set
            {
                if (!RequestBoundsChange(value)) return;

                OnPropertyChanging();
                _bounds = value;
                OnPropertyChanged();
            }
        }

        protected virtual bool RequestBoundsChange(Rectangle newBounds)
        {
            return true;
        }

        #endregion

        internal SnapObstacle(string name, Rectangle bounds, SnapRegion snapRegion, bool stickyEdges)
            : this(name, bounds, snapRegion, stickyEdges, DefaultSnapProximity, DefaultSnapDistance)
        {
        }

        internal SnapObstacle(string name, Rectangle bounds, SnapRegion snapRegion, bool stickyEdges, int snapProximity, int snapDistance)
        {
            this.Name = name;
            this.Bounds = bounds;
            this._snapRegion = snapRegion;
            this._stickyEdges = stickyEdges;
            this._snapProximity = snapProximity;
            this._snapDistance = snapDistance;
            this.Enabled = true;
            this.EnableSave = true;
        }
    }
}