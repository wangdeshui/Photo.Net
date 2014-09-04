using System.Drawing;
using Photo.Net.Core;
using Photo.Net.Gdi.Event;

namespace Photo.Net.Gdi.Surfaces
{
    /// <summary>
    /// This class handles rendering something to a SurfaceBox via a Graphics context.
    /// </summary>
    public abstract class SurfaceBoxGraphicsRenderer
        : SurfaceBoxRenderer
    {
        public SurfaceBoxGraphicsRenderer(SurfaceBoxRenderList ownerList)
            : base(ownerList)
        {
        }

        public abstract void RenderToGraphics(Graphics g, Point offset);

        public virtual bool ShouldRender()
        {
            return true;
        }

        public override sealed void Render(Surface dst, Point offset)
        {
            if (ShouldRender())
            {
                using (var ra = new RenderArgs(dst))
                {
                    RenderToGraphics(ra.Graphics, offset);
                }
            }
        }
    }
}
