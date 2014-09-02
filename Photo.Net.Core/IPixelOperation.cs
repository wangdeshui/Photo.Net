using System.Drawing;

namespace Photo.Net.Core
{
    /// <summary>
    /// Provides an interface for the methods that UnaryPixelOp and BinaryPixelOp share.
    /// For UnaryPixelOp, this produces the function, "dst = F(src)"
    /// For BinaryPixelOp, this produces the function, "dst = F(dst, src)"
    /// </summary>
    public interface IPixelOperation
    {

        /// <summary>
        /// This version of Apply has the liberty to decompose the rectangle of interest
        /// or do whatever types of optimizations it wants to with it. This is generally
        /// done to split the Apply operation into multiple threads.
        /// </summary>
        void Apply(Surface dst, Point dstOffset, Surface src, Point srcOffset, Size roiSize);

        /// <summary>
        /// This is the version of Apply that will always do exactly what you tell it do,
        /// without optimizations or otherwise.
        /// </summary>
        void ApplyBase(Surface dst, Point dstOffset, Surface src, Point srcOffset, Size roiSize);

        /// <summary>
        /// This version of Apply will perform on a scanline, not just a rectangle.
        /// </summary>
        void Apply(Surface dst, Point dstOffset, Surface src, Point srcOffset, int scanLength);
    }
}
