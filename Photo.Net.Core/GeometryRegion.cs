using System.Drawing;

namespace Photo.Net.Core
{
    /// <summary>
    /// Designed as a proxy to the GDI+ Region class.
    /// The main reason for having this right now is to work around some bugs in System.Drawing.Region,
    /// especially the memory leak in GetRegionScans().
    /// </summary>
    public class GeometryRegion
    {
        public Rectangle[] GetRegionScansReadOnlyInt()
        {
            return null;
        }
    }
}