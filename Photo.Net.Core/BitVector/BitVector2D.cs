using System;
using System.Collections;
using System.Drawing;
using Photo.Net.Core.Geometry;

namespace Photo.Net.Core.BitVector
{
    /// <summary>
    /// A bit array 2D table, usually use to record a bitmap changed pixel.
    /// </summary>
    public sealed class BitVector2D
        : IBitVector2D,
          ICloneable
    {
        private readonly BitArray _bitArray;

        public int Width { get; private set; }

        public int Height { get; private set; }

        public bool IsEmpty
        {
            get
            {
                return (Width == 0) || (Height == 0);
            }
        }

        public bool this[int x, int y]
        {
            get
            {
                CheckBounds(x, y);
                return _bitArray[x + (y * Width)];
            }

            set
            {
                CheckBounds(x, y);
                _bitArray[x + (y * Width)] = value;
            }
        }

        public bool this[Point pt]
        {
            get
            {
                CheckBounds(pt.X, pt.Y);
                return _bitArray[pt.X + (pt.Y * Width)];
            }

            set
            {
                CheckBounds(pt.X, pt.Y);
                _bitArray[pt.X + (pt.Y * Width)] = value;
            }
        }

        public BitVector2D(int width, int height)
        {
            this.Width = width;
            this.Height = height;
            this._bitArray = new BitArray(width * height, false);
        }

        public BitVector2D(BitVector2D copyMe)
        {
            this.Width = copyMe.Width;
            this.Height = copyMe.Height;
            this._bitArray = (BitArray)copyMe._bitArray.Clone();
        }

        private void CheckBounds(int x, int y)
        {
            if (x >= Width || y >= Height || x < 0 || y < 0)
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        public void Clear(bool newValue)
        {
            _bitArray.SetAll(newValue);
        }

        public bool Get(int x, int y)
        {
            return this[x, y];
        }

        public void Set(int x, int y, bool newValue)
        {
            this[x, y] = newValue;
        }

        public void Set(Point pt, bool newValue)
        {
            Set(pt.X, pt.Y, newValue);
        }

        public void Set(Rectangle rect, bool newValue)
        {
            for (int y = rect.Top; y < rect.Bottom; ++y)
            {
                for (int x = rect.Left; x < rect.Right; ++x)
                {
                    Set(x, y, newValue);
                }
            }
        }

        public void Set(Scanline scan, bool newValue)
        {
            int x = scan.X;
            while (x < scan.X + scan.Length)
            {
                Set(x, scan.Y, newValue);
                ++x;
            }
        }

        public void Set(GeometryRegion region, bool newValue)
        {
            foreach (Rectangle rect in region.GetRegionScansReadOnlyInt())
            {
                Set(rect, newValue);
            }
        }

        public void UnsafeSet(int x, int y, bool newValue)
        {
            _bitArray[x + (y * Width)] = newValue;
        }

        public void UnsafeSet(Point pt, bool newValue)
        {
            UnsafeSet(pt.X, pt.Y, newValue);
        }

        public void UnsafeSet(Rectangle rect, bool newValue)
        {
            for (int y = rect.Top; y < rect.Bottom; ++y)
            {
                for (int x = rect.Left; x < rect.Right; ++x)
                {
                    UnsafeSet(x, y, newValue);
                }
            }
        }

        public void UnsafeSet(Scanline scan, bool newValue)
        {
            int x = scan.X;
            while (x < scan.X + scan.Length)
            {
                UnsafeSet(x, scan.Y, newValue);
                ++x;
            }
        }

        public void UnsafeSet(GeometryRegion region, bool newValue)
        {
            foreach (Rectangle rect in region.GetRegionScansReadOnlyInt())
            {
                UnsafeSet(rect, newValue);
            }
        }

        public bool UnsafeGet(int x, int y)
        {
            return _bitArray[x + (y * Width)];
        }

        public void Invert(int x, int y)
        {
            Set(x, y, !Get(x, y));
        }

        public void Invert(Point pt)
        {
            Invert(pt.X, pt.Y);
        }

        public void Invert(Scanline scan)
        {
            int x = scan.X;

            while (x < scan.X + scan.Length)
            {
                Invert(x, scan.Y);
                ++x;
            }
        }

        public void Invert(Rectangle rect)
        {
            for (int y = rect.Top; y < rect.Bottom; ++y)
            {
                for (int x = rect.Left; x < rect.Right; ++x)
                {
                    Invert(x, y);
                }
            }
        }

        public void Invert(GeometryRegion region)
        {
            foreach (Rectangle rect in region.GetRegionScansReadOnlyInt())
            {
                Invert(rect);
            }
        }

        public void UnsafeInvert(int x, int y)
        {
            UnsafeSet(x, y, !UnsafeGet(x, y));
        }


        public void UnsafeInvert(Point pt)
        {
            UnsafeInvert(pt.X, pt.Y);
        }

        public void UnsafeInvert(Rectangle rect)
        {
            for (int y = rect.Top; y < rect.Bottom; ++y)
            {
                for (int x = rect.Left; x < rect.Right; ++x)
                {
                    UnsafeInvert(x, y);
                }
            }
        }

        public void UnsafeInvert(Scanline scan)
        {
            int x = scan.X;

            while (x < scan.X + scan.Length)
            {
                UnsafeInvert(x, scan.Y);
                ++x;
            }
        }

        public object Clone()
        {
            return new BitVector2D(this);
        }
    }
}