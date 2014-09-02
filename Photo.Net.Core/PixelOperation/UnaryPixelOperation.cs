using System;
using System.Drawing;
using Photo.Net.Core.Color;

namespace Photo.Net.Core.PixelOperation
{
    /// <summary>
    /// Defines a way to operate on a pixel, or a region of pixels, in a unary fashion.
    /// That is, it is a simple function F that takes one parameter and returns a
    /// result of the form: d = F(c)
    /// </summary>
    [Serializable]
    public unsafe abstract class UnaryPixelOperation
        : PixelOperation
    {
        public abstract ColorBgra Apply(ColorBgra color);

        public override void Apply(ColorBgra* dst, ColorBgra* src, int length)
        {
            while (length > 0)
            {
                *dst = Apply(*src);
                ++dst;
                ++src;
                --length;
            }
        }

        public virtual void Apply(ColorBgra* ptr, int length)
        {
            while (length > 0)
            {
                *ptr = Apply(*ptr);
                ++ptr;
                --length;
            }
        }

        private void ApplyRectangle(Surface surface, Rectangle rect)
        {
            for (int y = rect.Top; y < rect.Bottom; ++y)
            {
                ColorBgra* ptr = surface.GetPointAddress(rect.Left, y);
                Apply(ptr, rect.Width);
            }
        }

        public void Apply(Surface surface, Rectangle[] roi, int startIndex, int length)
        {
            Rectangle regionBounds = Utility.GetRegionBounds(roi, startIndex, length);

            if (regionBounds != Rectangle.Intersect(surface.Bounds, regionBounds))
            {
                throw new ArgumentOutOfRangeException("roi", "Region is out of bounds");
            }

            unsafe
            {
                for (int x = startIndex; x < startIndex + length; ++x)
                {
                    ApplyRectangle(surface, roi[x]);
                }
            }
        }

        public void Apply(Surface surface, Rectangle[] roi)
        {
            Apply(surface, roi, 0, roi.Length);
        }

        public void Apply(Surface surface, RectangleF[] roiF, int startIndex, int length)
        {
            Rectangle regionBounds = Rectangle.Truncate(Utility.GetRegionBounds(roiF, startIndex, length));

            if (regionBounds != Rectangle.Intersect(surface.Bounds, regionBounds))
            {
                throw new ArgumentOutOfRangeException("roiF", "Region is out of bounds");
            }

            unsafe
            {
                for (int x = startIndex; x < startIndex + length; ++x)
                {
                    ApplyRectangle(surface, Rectangle.Truncate(roiF[x]));
                }
            }
        }

        public void Apply(Surface surface, RectangleF[] roiF)
        {
            Apply(surface, roiF, 0, roiF.Length);
        }

        public unsafe void Apply(Surface surface, Rectangle roi)
        {
            ApplyRectangle(surface, roi);
        }

        public void Apply(Surface surface, Scanline scan)
        {
            Apply(surface.GetPointAddress(scan.X, scan.Y), scan.Length);
        }

        public void Apply(Surface surface, Scanline[] scans)
        {
            foreach (Scanline scan in scans)
            {
                Apply(surface, scan);
            }
        }

        public override void Apply(Surface dst, Point dstOffset, Surface src, Point srcOffset, int scanLength)
        {
            Apply(dst.GetPointAddress(dstOffset), src.GetPointAddress(srcOffset), scanLength);
        }

        public void Apply(Surface dst, Surface src, Rectangle roi)
        {
            for (int y = roi.Top; y < roi.Bottom; ++y)
            {
                ColorBgra* dstPtr = dst.GetPointAddress(roi.Left, y);
                ColorBgra* srcPtr = src.GetPointAddress(roi.Left, y);
                Apply(dstPtr, srcPtr, roi.Width);
            }
        }

        public void Apply(Surface surface, GeometryRegion roi)
        {
            Apply(surface, roi.GetRegionScansReadOnlyInt());
        }
    }
}