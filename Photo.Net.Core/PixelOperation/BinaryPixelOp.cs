using System;
using System.Drawing;
using Photo.Net.Core.Color;

namespace Photo.Net.Core.PixelOperation
{
    /// <summary>
    /// Defines a way to operate on a pixel, or a region of pixels, in a binary fashion.
    /// That is, it is a simple function F that takes two parameters and returns a
    /// result of the form: c = F(a, b)
    /// </summary>
    [Serializable]
    public unsafe abstract class BinaryPixelOp
        : PixelOperation
    {
        public abstract ColorBgra Apply(ColorBgra lhs, ColorBgra rhs);

        public virtual void Apply(ColorBgra* dst, ColorBgra* lhs, ColorBgra* rhs, int length)
        {
            while (length > 0)
            {
                *dst = Apply(*lhs, *rhs);
                ++dst;
                ++lhs;
                ++rhs;
                --length;
            }
        }

        /// <summary>
        /// Provides a default implementation for performing dst = F(lhs, rhs) over some rectangle of interest.
        /// </summary>
        /// <param name="dst">The Surface to write pixels to.</param>
        /// <param name="dstOffset">The pixel offset that defines the upper-left of the rectangle-of-interest for the dst Surface.</param>
        /// <param name="lhs">The Surface to read pixels from for the lhs parameter given to the method <b>ColorBgra Apply(ColorBgra, ColorBgra)</b>b>.</param></param>
        /// <param name="lhsOffset">The pixel offset that defines the upper-left of the rectangle-of-interest for the lhs Surface.</param>
        /// <param name="rhs">The Surface to read pixels from for the rhs parameter given to the method <b>ColorBgra Apply(ColorBgra, ColorBgra)</b></param>
        /// <param name="rhsOffset">The pixel offset that defines the upper-left of the rectangle-of-interest for the rhs Surface.</param>
        /// <param name="roiSize">The size of the rectangles-of-interest for all Surfaces.</param>
        public void Apply(Surface dst, Point dstOffset,
                          Surface lhs, Point lhsOffset,
                          Surface rhs, Point rhsOffset,
                          Size roiSize)
        {
            // Cache the width and height properties
            int width = roiSize.Width;
            int height = roiSize.Height;

            // Do the work.
            unsafe
            {
                for (int row = 0; row < height; ++row)
                {
                    ColorBgra* dstPtr = dst.GetPointAddress(dstOffset.X, dstOffset.Y + row);
                    ColorBgra* lhsPtr = lhs.GetPointAddress(lhsOffset.X, lhsOffset.Y + row);
                    ColorBgra* rhsPtr = rhs.GetPointAddress(rhsOffset.X, rhsOffset.Y + row);

                    Apply(dstPtr, lhsPtr, rhsPtr, width);
                }
            }
        }

        public override void Apply(ColorBgra* dst, ColorBgra* src, int length)
        {
            while (length > 0)
            {
                *dst = Apply(*dst, *src);
                ++dst;
                ++src;
                --length;
            }
        }

        public override void Apply(Surface dst, Point dstOffset, Surface src, Point srcOffset, int roiLength)
        {
            Apply(dst.GetPointAddress(dstOffset), src.GetPointAddress(srcOffset), roiLength);
        }

        public void Apply(Surface dst, Surface src)
        {
            if (dst.Size != src.Size)
            {
                throw new ArgumentException("dst.Size != src.Size");
            }

            for (int y = 0; y < dst.Height; ++y)
            {
                ColorBgra* dstPtr = dst.UnsafeGetRowAddress(y);
                ColorBgra* srcPtr = src.UnsafeGetRowAddress(y);
                Apply(dstPtr, srcPtr, dst.Width);
            }
        }

        public void Apply(Surface dst, Surface lhs, Surface rhs)
        {
            if (dst.Size != lhs.Size)
            {
                throw new ArgumentException("dst.Size != lhs.Size");
            }

            if (lhs.Size != rhs.Size)
            {
                throw new ArgumentException("lhs.Size != rhs.Size");
            }

            for (int y = 0; y < dst.Height; ++y)
            {
                ColorBgra* rhsPtr = rhs.UnsafeGetRowAddress(y);
                ColorBgra* dstPtr = dst.UnsafeGetRowAddress(y);
                ColorBgra* lhsPtr = lhs.UnsafeGetRowAddress(y);

                Apply(dstPtr, lhsPtr, rhsPtr, dst.Width);
            }
        }
    }
}
