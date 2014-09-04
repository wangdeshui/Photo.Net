using System;

namespace Photo.Net.Tool.Snap
{
    public sealed class SnapDescription
    {
        private readonly SnapObstacle _snappedTo;

        public SnapObstacle SnappedTo
        {
            get
            {
                return this._snappedTo;
            }
        }

        public HorizontalSnapEdge HorizontalEdge { get; set; }

        public VerticalSnapEdge VerticalEdge { get; set; }

        public int XOffset { get; set; }

        public int YOffset { get; set; }

        public SnapDescription(
            SnapObstacle snappedTo,
            HorizontalSnapEdge horizontalEdge,
            VerticalSnapEdge verticalEdge,
            int xOffset,
            int yOffset)
        {
            if (snappedTo == null)
            {
                throw new ArgumentNullException("snappedTo");
            }

            this._snappedTo = snappedTo;
            this.HorizontalEdge = horizontalEdge;
            this.VerticalEdge = verticalEdge;
            this.XOffset = xOffset;
            this.YOffset = yOffset;
        }
    }
}
