using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace Photo.Net.Core.Color
{
    /// <summary>
    /// A table store 255 indexed colors.
    /// </summary>
    public class IndexedColorTable
    {
        private readonly Dictionary<int, ColorBgra> _colors = new Dictionary<int, ColorBgra>(255);

        public ColorBgra this[int index]
        {
            get { return _colors[index]; }
            set
            {
                if (_colors.ContainsKey(index) || _colors.Count < 255)
                {
                    _colors[index] = value;
                }
                else if (_colors.Count >= 255)
                {
                    throw new IndexOutOfRangeException("Only store 255 indexed colors.");
                }
            }
        }

        public bool Add(int index, ColorBgra newColor)
        {
            if (_colors.Count >= 255)
            {
                return false;
            }

            _colors.Add(index, newColor);
            return true;
        }

        public bool Contain(ColorBgra color)
        {
            return _colors.Any(x => x.Value == color);
        }

        public static IndexedColorTable FromBitmap(Bitmap bmp)
        {
            int index = 0;
            var table = new IndexedColorTable();
            unsafe
            {
                BitmapData bData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                try
                {
                    for (int y = 0; y < bData.Height; ++y)
                    {
                        byte* srcPtr = (byte*)bData.Scan0.ToPointer() + (y * bData.Stride);

                        for (int x = 0; x < bData.Width; ++x)
                        {
                            byte b = *srcPtr;
                            byte g = *(srcPtr + 1);
                            byte r = *(srcPtr + 2);
                            var color = ColorBgra.FromBgra(b, g, r, 255);

                            if (!table.Contain(color))
                            {
                                table[index] = color;
                                index++;
                            }
                        }
                    }
                }

                finally
                {
                    bmp.UnlockBits(bData);
                }
            }

            return table;
        }
    }
}
