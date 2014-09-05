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
        private ColorPalette _colorPalette = GetColorPalette();

        public ColorPalette ColorPalette
        {
            get { return _colorPalette; }
            set { _colorPalette = value; }
        }

        public System.Drawing.Color this[int index]
        {
            get { return _colorPalette.Entries[index]; }
            set { _colorPalette.Entries[index] = value; }
        }

        public bool Contain(System.Drawing.Color color)
        {
            return _colorPalette.Entries.Any(x => x == color);
        }

        public static IndexedColorTable FromBitmap(Bitmap bmp)
        {
            var table = new IndexedColorTable() { ColorPalette = bmp.Palette };
            return table;

            //            int index = 0;
            //            var table = new IndexedColorTable();
            //            unsafe
            //            {
            //                BitmapData bData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            //                try
            //                {
            //                    for (int y = 0; y < bData.Height; ++y)
            //                    {
            //                        byte* srcPtr = (byte*)bData.Scan0.ToPointer() + (y * bData.Stride);
            //
            //                        for (int x = 0; x < bData.Width; ++x)
            //                        {
            //                            byte b = *srcPtr;
            //                            byte g = *(srcPtr + 1);
            //                            byte r = *(srcPtr + 2);
            //                            var color = System.Drawing.Color.FromArgb(255, r, g, b);
            //
            //                            if (!table.Contain(color))
            //                            {
            //                                table[index] = color;
            //                                index++;
            //                            }
            //                        }
            //                    }
            //
            //                    if (!table.Contain(System.Drawing.Color.Transparent) && index < 255)
            //                        table[index] = System.Drawing.Color.Transparent;
            //                }
            //
            //                finally
            //                {
            //                    bmp.UnlockBits(bData);
            //                }
            //            }
        }

        public static ColorPalette GetColorPalette(uint colorCount = 255)
        {
            var bitscolordepth = PixelFormat.Format1bppIndexed;
            if (colorCount > 2)
                bitscolordepth = PixelFormat.Format4bppIndexed;
            if (colorCount > 16)
                bitscolordepth = PixelFormat.Format8bppIndexed;

            var bitmap = new Bitmap(1, 1, bitscolordepth);
            ColorPalette palette = bitmap.Palette;
            bitmap.Dispose();
            return palette;
        }
    }
}
