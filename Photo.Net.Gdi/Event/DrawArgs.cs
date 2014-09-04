using System;
using System.Drawing;

namespace Photo.Net.Gdi.Event
{
    /// <summary>
    /// Steal control paint event args.
    /// </summary>
    public sealed class DrawArgs
        : EventArgs
    {
        public Graphics Graphics { get; private set; }

        public Rectangle ClipRectangle { get; private set; }

        public DrawArgs(Graphics graphics, Rectangle clipRectangle)
        {
            this.Graphics = graphics;
            this.ClipRectangle = clipRectangle;
        }
    }

    public delegate void DrawEventHandler(object sender, DrawArgs e);
}
