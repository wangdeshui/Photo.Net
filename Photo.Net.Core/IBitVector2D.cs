using System.Drawing;

namespace Photo.Net.Core
{
    /// <summary>
    /// A bit array 2D table, usually use to record a bitmap changed pixel.
    /// </summary>
    public interface IBitVector2D
    {
        int Width { get; }

        int Height { get; }

        bool this[int x, int y] { get; set; }

        bool this[Point pt] { get; set; }

        bool IsEmpty { get; }

        void Clear(bool newValue);

        void Set(int x, int y, bool newValue);
        void Set(Point pt, bool newValue);
        void Set(Rectangle rect, bool newValue);
        void Set(Scanline scan, bool newValue);
        void Set(GeometryRegion region, bool newValue);
        void UnsafeSet(int x, int y, bool newValue);

        bool Get(int x, int y);
        bool UnsafeGet(int x, int y);

        void Invert(int x, int y);
        void Invert(Point pt);
        void Invert(Rectangle rect);
        void Invert(Scanline scan);
        void Invert(GeometryRegion region);
    }
}
