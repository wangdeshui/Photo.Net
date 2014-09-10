using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Photo.Net.Core.Area;
using Photo.Net.Core.Struct;

namespace Photo.Net.Core
{
    /// <summary>
    /// Defines miscellaneous constants and static functions.
    /// </summary>
    /// // TODO: refactor into mini static classes
    public static class Utility
    {

        public static bool IsNumber(float x)
        {
            return x >= float.MinValue && x <= float.MaxValue;
        }

        public static bool IsNumber(double x)
        {
            return x >= double.MinValue && x <= double.MaxValue;
        }

        #region Lerp

        public static float Lerp(float from, float to, float frac)
        {
            return (from + frac * (to - from));
        }

        public static double Lerp(double from, double to, double frac)
        {
            return (from + frac * (to - from));
        }

        public static PointF Lerp(PointF from, PointF to, float frac)
        {
            return new PointF(Lerp(from.X, to.X, frac), Lerp(from.Y, to.Y, frac));
        }

        #endregion

        public static byte FastScaleByteByByte(byte a, byte b)
        {
            int r1 = a * b + 0x80;
            int r2 = ((r1 >> 8) + r1) >> 8;
            return (byte)r2;
        }

        #region Clamp

        public static double Clamp(this double x, double min, double max)
        {
            if (x < min)
            {
                return min;
            }
            if (x > max)
            {
                return max;
            }

            return x;
        }

        public static float Clamp(this float x, float min, float max)
        {
            if (x < min)
            {
                return min;
            }
            else if (x > max)
            {
                return max;
            }
            else
            {
                return x;
            }
        }

        public static int Clamp(this int x, int min, int max)
        {
            if (x < min)
            {
                return min;
            }
            else if (x > max)
            {
                return max;
            }
            else
            {
                return x;
            }
        }

        public static byte ClampToByte(this double x)
        {
            if (x > 255)
            {
                return 255;
            }
            else if (x < 0)
            {
                return 0;
            }
            else
            {
                return (byte)x;
            }
        }

        public static byte ClampToByte(this float x)
        {
            if (x > 255)
            {
                return 255;
            }
            else if (x < 0)
            {
                return 0;
            }
            else
            {
                return (byte)x;
            }
        }

        public static byte ClampToByte(this int x)
        {
            if (x > 255)
            {
                return 255;
            }
            else if (x < 0)
            {
                return 0;
            }
            else
            {
                return (byte)x;
            }
        }

        #endregion

        public static bool AllowGcFullCollect = true;
        public static void GcFullCollect()
        {
            if (AllowGcFullCollect)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        public static int ReadFromStream(Stream input, byte[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;

            while (totalBytesRead < count)
            {
                int bytesRead = input.Read(buffer, offset + totalBytesRead, count - totalBytesRead);

                if (bytesRead == 0)
                {
                    throw new IOException("ran out of data");
                }

                totalBytesRead += bytesRead;
            }

            return totalBytesRead;
        }

        public static void SplitRectangle(Rectangle rect, Rectangle[] rects)
        {
            int height = rect.Height;

            for (int i = 0; i < rects.Length; ++i)
            {
                Rectangle newRect = Rectangle.FromLTRB(rect.Left,
                                                       rect.Top + ((height * i) / rects.Length),
                                                       rect.Right,
                                                       rect.Top + ((height * (i + 1)) / rects.Length));

                rects[i] = newRect;
            }
        }

        /// <summary>
        /// Converts a RectangleF to RectangleF by rounding down the Location and rounding
        /// up the Size.
        /// </summary>
        public static Rectangle RoundRectangle(RectangleF rectF)
        {
            var left = (float)Math.Floor(rectF.Left);
            var top = (float)Math.Floor(rectF.Top);
            var right = (float)Math.Ceiling(rectF.Right);
            var bottom = (float)Math.Ceiling(rectF.Bottom);

            return Rectangle.Truncate(RectangleF.FromLTRB(left, top, right, bottom));
        }

        public static Size ComputeThumbnailSize(Size originalSize, int maxEdgeLength)
        {
            Size thumbSize;

            if (originalSize.Width > originalSize.Height)
            {
                int longSide = Math.Min(originalSize.Width, maxEdgeLength);
                thumbSize = new Size(longSide, Math.Max(1, (originalSize.Height * longSide) / originalSize.Width));
            }
            else if (originalSize.Height > originalSize.Width)
            {
                int longSide = Math.Min(originalSize.Height, maxEdgeLength);
                thumbSize = new Size(Math.Max(1, (originalSize.Width * longSide) / originalSize.Height), longSide);
            }
            else // if (docSize.Width == docSize.Height)
            {
                int longSide = Math.Min(originalSize.Width, maxEdgeLength);
                thumbSize = new Size(longSide, longSide);
            }

            return thumbSize;
        }

        #region GetRegionBounds

        /// <summary>
        /// Allows you to find the bounding box for a Region object without requiring
        /// the presence of a Graphics object.
        /// (Region.GetBounds takes a Graphics instance as its only parameter.)
        /// </summary>
        /// <param name="region">The region you want to find a bounding box for.</param>
        /// <returns>A RectangleF structure that surrounds the Region.</returns>
        public static Rectangle GetRegionBounds(GeometryRegion region)
        {
            Rectangle[] rects = region.GetRegionScansReadOnlyInt();
            return GetRegionBounds(rects, 0, rects.Length);
        }

        /// <summary>
        /// Allows you to find the bounding box for a "region" that is described as an
        /// array of bounding boxes.
        /// </summary>
        /// <param name="rectsF">The "region" you want to find a bounding box for.</param>
        /// <returns>A RectangleF structure that surrounds the Region.</returns>
        public static RectangleF GetRegionBounds(RectangleF[] rectsF, int startIndex, int length)
        {
            if (rectsF.Length == 0)
            {
                return RectangleF.Empty;
            }

            float left = rectsF[startIndex].Left;
            float top = rectsF[startIndex].Top;
            float right = rectsF[startIndex].Right;
            float bottom = rectsF[startIndex].Bottom;

            for (int i = startIndex + 1; i < startIndex + length; ++i)
            {
                RectangleF rectF = rectsF[i];

                if (rectF.Left < left)
                {
                    left = rectF.Left;
                }

                if (rectF.Top < top)
                {
                    top = rectF.Top;
                }

                if (rectF.Right > right)
                {
                    right = rectF.Right;
                }

                if (rectF.Bottom > bottom)
                {
                    bottom = rectF.Bottom;
                }
            }

            return RectangleF.FromLTRB(left, top, right, bottom);
        }

        /// <summary>
        /// Allows you to find the bounding box for a "region" that is described as an
        /// array of bounding boxes.
        /// </summary>
        /// <returns>A RectangleF structure that surrounds the Region.</returns>
        public static Rectangle GetRegionBounds(Rectangle[] rects, int startIndex, int length)
        {
            if (rects.Length == 0)
            {
                return Rectangle.Empty;
            }

            int left = rects[startIndex].Left;
            int top = rects[startIndex].Top;
            int right = rects[startIndex].Right;
            int bottom = rects[startIndex].Bottom;

            for (int i = startIndex + 1; i < startIndex + length; ++i)
            {
                Rectangle rect = rects[i];

                if (rect.Left < left)
                {
                    left = rect.Left;
                }

                if (rect.Top < top)
                {
                    top = rect.Top;
                }

                if (rect.Right > right)
                {
                    right = rect.Right;
                }

                if (rect.Bottom > bottom)
                {
                    bottom = rect.Bottom;
                }
            }

            return Rectangle.FromLTRB(left, top, right, bottom);
        }

        public static RectangleF GetRegionBounds(RectangleF[] rectsF)
        {
            return GetRegionBounds(rectsF, 0, rectsF.Length);
        }

        public static Rectangle GetRegionBounds(Rectangle[] rects)
        {
            return GetRegionBounds(rects, 0, rects.Length);
        }

        #endregion

        #region TranslatePointsInPlace

        public static void TranslatePointsInPlace(PointF[] ptsF, float dx, float dy)
        {
            for (int i = 0; i < ptsF.Length; ++i)
            {
                ptsF[i].X += dx;
                ptsF[i].Y += dy;
            }
        }

        public static void TranslatePointsInPlace(Point[] pts, int dx, int dy)
        {
            for (int i = 0; i < pts.Length; ++i)
            {
                pts[i].X += dx;
                pts[i].Y += dy;
            }
        }

        #endregion

        #region GetScans

        public static Scanline[] GetScans(Point[] vertices)
        {
            return GetScans(vertices, 0, vertices.Length);
        }

        public static Scanline[] GetScans(Point[] vertices, int startIndex, int length)
        {
            if (length > vertices.Length - startIndex)
            {
                throw new ArgumentException("out of bounds: length > vertices.Length - startIndex");
            }

            int ymax = 0;

            // Build edge table
            var edgeTable = new Edge[length];
            int edgeCount = 0;

            for (int i = startIndex; i < startIndex + length; ++i)
            {
                Point top = vertices[i];
                Point bottom = vertices[(((i + 1) - startIndex) % length) + startIndex];

                if (top.Y > bottom.Y)
                {
                    Point temp = top;
                    top = bottom;
                    bottom = temp;
                }

                int dy = bottom.Y - top.Y;

                if (dy != 0)
                {
                    edgeTable[edgeCount] = new Edge(top.Y, bottom.Y, top.X << 8, (((bottom.X - top.X) << 8) / dy));
                    ymax = Math.Max(ymax, bottom.Y);
                    ++edgeCount;
                }
            }

            // Sort edge table by miny
            for (int i = 0; i < edgeCount - 1; ++i)
            {
                int min = i;

                for (int j = i + 1; j < edgeCount; ++j)
                {
                    if (edgeTable[j].Miny < edgeTable[min].Miny)
                    {
                        min = j;
                    }
                }

                if (min != i)
                {
                    Edge temp = edgeTable[min];
                    edgeTable[min] = edgeTable[i];
                    edgeTable[i] = temp;
                }
            }

            // Compute how many scanlines we will be emitting
            int scanCount = 0;
            int activeLow = 0;
            int activeHigh = 0;
            int yscan1 = edgeTable[0].Miny;

            // we assume that edgeTable[0].miny == yscan
            while (activeHigh < edgeCount - 1 &&
                   edgeTable[activeHigh + 1].Miny == yscan1)
            {
                ++activeHigh;
            }

            while (yscan1 <= ymax)
            {
                // Find new edges where yscan == miny
                while (activeHigh < edgeCount - 1 &&
                       edgeTable[activeHigh + 1].Miny == yscan1)
                {
                    ++activeHigh;
                }

                int count = 0;
                for (int i = activeLow; i <= activeHigh; ++i)
                {
                    if (edgeTable[i].Maxy > yscan1)
                    {
                        ++count;
                    }
                }

                scanCount += count / 2;
                ++yscan1;

                // Remove edges where yscan == maxy
                while (activeLow < edgeCount - 1 &&
                       edgeTable[activeLow].Maxy <= yscan1)
                {
                    ++activeLow;
                }

                if (activeLow > activeHigh)
                {
                    activeHigh = activeLow;
                }
            }

            // Allocate scanlines that we'll return
            var scans = new Scanline[scanCount];

            // Active Edge Table (AET): it is indices into the Edge Table (ET)
            var active = new int[edgeCount];
            int activeCount = 0;
            int yscan2 = edgeTable[0].Miny;
            int scansIndex = 0;

            // Repeat until both the ET and AET are empty
            while (yscan2 <= ymax)
            {
                // Move any edges from the ET to the AET where yscan == miny
                for (int i = 0; i < edgeCount; ++i)
                {
                    if (edgeTable[i].Miny == yscan2)
                    {
                        active[activeCount] = i;
                        ++activeCount;
                    }
                }

                // Sort the AET on x
                for (int i = 0; i < activeCount - 1; ++i)
                {
                    int min = i;

                    for (int j = i + 1; j < activeCount; ++j)
                    {
                        if (edgeTable[active[j]].X < edgeTable[active[min]].X)
                        {
                            min = j;
                        }
                    }

                    if (min != i)
                    {
                        int temp = active[min];
                        active[min] = active[i];
                        active[i] = temp;
                    }
                }

                // For each pair of entries in the AET, fill in pixels between their info
                for (int i = 0; i < activeCount; i += 2)
                {
                    Edge el = edgeTable[active[i]];
                    Edge er = edgeTable[active[i + 1]];
                    int startx = (el.X + 0xff) >> 8; // ceil(x)
                    int endx = er.X >> 8; // floor(x)

                    scans[scansIndex] = new Scanline(startx, yscan2, endx - startx);
                    ++scansIndex;
                }

                ++yscan2;

                // Remove from the AET any edge where yscan == maxy
                int k = 0;
                while (k < activeCount && activeCount > 0)
                {
                    if (edgeTable[active[k]].Maxy == yscan2)
                    {
                        // remove by shifting everything down one
                        for (int j = k + 1; j < activeCount; ++j)
                        {
                            active[j - 1] = active[j];
                        }

                        --activeCount;
                    }
                    else
                    {
                        ++k;
                    }
                }

                // Update x for each entry in AET
                for (int i = 0; i < activeCount; ++i)
                {
                    edgeTable[active[i]].X += edgeTable[active[i]].Dxdy;
                }
            }

            return scans;
        }

        #endregion

        #region RectanglesToRegion

        public static GeometryRegion RectanglesToRegion(RectangleF[] rectsF, int startIndex, int length)
        {
            GeometryRegion region;

            if (rectsF == null || rectsF.Length == 0 || length == 0)
            {
                region = GeometryRegion.CreateEmpty();
            }
            else
            {
                using (var path = new GeometryGraphicsPath())
                {
                    path.FillMode = FillMode.Winding;

                    if (startIndex == 0 && length == rectsF.Length)
                    {
                        path.AddRectangles(rectsF);
                    }
                    else
                    {
                        for (int i = startIndex; i < startIndex + length; ++i)
                        {
                            path.AddRectangle(rectsF[i]);
                        }
                    }

                    region = new GeometryRegion(path);
                }
            }

            return region;
        }

        public static GeometryRegion RectanglesToRegion(RectangleF[] rectsF)
        {
            return RectanglesToRegion(rectsF, 0, rectsF != null ? rectsF.Length : 0);
        }

        public static GeometryRegion RectanglesToRegion(RectangleF[] rectsF1, RectangleF[] rectsF2,
            params RectangleF[][] rectsFA)
        {
            using (var path = new GeometryGraphicsPath())
            {
                path.FillMode = FillMode.Winding;

                if (rectsF1 != null && rectsF1.Length > 0)
                {
                    path.AddRectangles(rectsF1);
                }

                if (rectsF2 != null && rectsF2.Length > 0)
                {
                    path.AddRectangles(rectsF2);
                }

                foreach (RectangleF[] rectsF in rectsFA)
                {
                    if (rectsF != null && rectsF.Length > 0)
                    {
                        path.AddRectangles(rectsF);
                    }
                }

                return new GeometryRegion(path);
            }
        }

        public static GeometryRegion RectanglesToRegion(Rectangle[] rects, int startIndex, int length)
        {
            GeometryRegion region;

            if (length == 0)
            {
                region = GeometryRegion.CreateEmpty();
            }
            else
            {
                using (var path = new GeometryGraphicsPath())
                {
                    path.FillMode = FillMode.Winding;
                    if (startIndex == 0 && length == rects.Length)
                    {
                        path.AddRectangles(rects);
                    }
                    else
                    {
                        for (int i = startIndex; i < startIndex + length; ++i)
                        {
                            path.AddRectangle(rects[i]);
                        }
                    }

                    region = new GeometryRegion(path);
                    path.Dispose();
                }
            }

            return region;
        }

        public static GeometryRegion RectanglesToRegion(Rectangle[] rects)
        {
            return RectanglesToRegion(rects, 0, rects.Length);
        }

        #endregion

        public static string GetStaticName(Type type)
        {
            PropertyInfo pi = type.GetProperty("StaticName", BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty);
            return (string)pi.GetValue(null, null);
        }

        public static Control FindFocus()
        {
            return (from Form form in Application.OpenForms select FindFocus(form)).FirstOrDefault(focused => focused != null);
        }

        private static Control FindFocus(Control c)
        {
            if (c.Focused)
            {
                return c;
            }

            return (from Control child in c.Controls select FindFocus(child)).FirstOrDefault(f => f != null);
        }


        #region ImageToIcon

        public static readonly System.Drawing.Color TransparentKey = System.Drawing.Color.FromArgb(192, 192, 192);

        public static Icon ImageToIcon(Image image)
        {
            return ImageToIcon(image, TransparentKey);
        }

        public static Icon ImageToIcon(Image image, bool disposeImage)
        {
            return ImageToIcon(image, TransparentKey, disposeImage);
        }

        public static Icon ImageToIcon(Image image, System.Drawing.Color seeThru)
        {
            return ImageToIcon(image, seeThru, false);
        }

        /// <summary>
        /// Converts an Image to an Icon.
        /// </summary>
        /// <param name="image">The Image to convert to an icon. Must be an appropriate icon size (32x32, 16x16, etc).</param>
        /// <param name="seeThru">The color that will be treated as transparent in the icon.</param>
        /// <param name="disposeImage">Whether or not to dispose the passed-in Image.</param>
        /// <returns>An Icon representation of the Image.</returns>
        public static Icon ImageToIcon(Image image, System.Drawing.Color seeThru, bool disposeImage)
        {
            var bitmap = new Bitmap(image);

            for (int y = 0; y < bitmap.Height; ++y)
            {
                for (int x = 0; x < bitmap.Width; ++x)
                {
                    if (bitmap.GetPixel(x, y) == seeThru)
                    {
                        bitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(0));
                    }
                }
            }

            Icon icon = Icon.FromHandle(bitmap.GetHicon());
            bitmap.Dispose();

            if (disposeImage)
            {
                image.Dispose();
            }

            return icon;
        }

        #endregion

        public static bool IsArrowKey(Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;

            if (key == Keys.Up || key == Keys.Down || key == Keys.Left || key == Keys.Right)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void ErrorBox(IWin32Window parent, string message)
        {
            //            MessageBox.Show(parent, message, PtnInfo.GetBareProductName(), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static bool AllowGCFullCollect = true;
        public static void GCFullCollect()
        {
            if (AllowGCFullCollect)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }


        public static Point GetRectangleCenter(Rectangle rect)
        {
            return new Point((rect.Left + rect.Right) / 2, (rect.Top + rect.Bottom) / 2);
        }

        public static PointF GetRectangleCenter(RectangleF rect)
        {
            return new PointF((rect.Left + rect.Right) / 2, (rect.Top + rect.Bottom) / 2);
        }


        public static int FastDivideShortByByte(ushort n, byte d)
        {
            int i = d * 3;
            uint m = masTable[i];
            uint a = masTable[i + 1];
            uint s = masTable[i + 2];

            uint nTimesMPlusA = unchecked((n * m) + a);
            uint shifted = nTimesMPlusA >> (int)s;
            int r = (int)shifted;

            return r;
        }

        #region masTable
        private static readonly uint[] masTable = 
        {
            0x00000000, 0x00000000, 0,  // 0
            0x00000001, 0x00000000, 0,  // 1
            0x00000001, 0x00000000, 1,  // 2
            0xAAAAAAAB, 0x00000000, 33, // 3
            0x00000001, 0x00000000, 2,  // 4
            0xCCCCCCCD, 0x00000000, 34, // 5
            0xAAAAAAAB, 0x00000000, 34, // 6
            0x49249249, 0x49249249, 33, // 7
            0x00000001, 0x00000000, 3,  // 8
            0x38E38E39, 0x00000000, 33, // 9
            0xCCCCCCCD, 0x00000000, 35, // 10
            0xBA2E8BA3, 0x00000000, 35, // 11
            0xAAAAAAAB, 0x00000000, 35, // 12
            0x4EC4EC4F, 0x00000000, 34, // 13
            0x49249249, 0x49249249, 34, // 14
            0x88888889, 0x00000000, 35, // 15
            0x00000001, 0x00000000, 4,  // 16
            0xF0F0F0F1, 0x00000000, 36, // 17
            0x38E38E39, 0x00000000, 34, // 18
            0xD79435E5, 0xD79435E5, 36, // 19
            0xCCCCCCCD, 0x00000000, 36, // 20
            0xC30C30C3, 0xC30C30C3, 36, // 21
            0xBA2E8BA3, 0x00000000, 36, // 22
            0xB21642C9, 0x00000000, 36, // 23
            0xAAAAAAAB, 0x00000000, 36, // 24
            0x51EB851F, 0x00000000, 35, // 25
            0x4EC4EC4F, 0x00000000, 35, // 26
            0x97B425ED, 0x97B425ED, 36, // 27
            0x49249249, 0x49249249, 35, // 28
            0x8D3DCB09, 0x00000000, 36, // 29
            0x88888889, 0x00000000, 36, // 30
            0x42108421, 0x42108421, 35, // 31
            0x00000001, 0x00000000, 5,  // 32
            0x3E0F83E1, 0x00000000, 35, // 33
            0xF0F0F0F1, 0x00000000, 37, // 34
            0x75075075, 0x75075075, 36, // 35
            0x38E38E39, 0x00000000, 35, // 36
            0x6EB3E453, 0x6EB3E453, 36, // 37
            0xD79435E5, 0xD79435E5, 37, // 38
            0x69069069, 0x69069069, 36, // 39
            0xCCCCCCCD, 0x00000000, 37, // 40
            0xC7CE0C7D, 0x00000000, 37, // 41
            0xC30C30C3, 0xC30C30C3, 37, // 42
            0x2FA0BE83, 0x00000000, 35, // 43
            0xBA2E8BA3, 0x00000000, 37, // 44
            0x5B05B05B, 0x5B05B05B, 36, // 45
            0xB21642C9, 0x00000000, 37, // 46
            0xAE4C415D, 0x00000000, 37, // 47
            0xAAAAAAAB, 0x00000000, 37, // 48
            0x5397829D, 0x00000000, 36, // 49
            0x51EB851F, 0x00000000, 36, // 50
            0xA0A0A0A1, 0x00000000, 37, // 51
            0x4EC4EC4F, 0x00000000, 36, // 52
            0x9A90E7D9, 0x9A90E7D9, 37, // 53
            0x97B425ED, 0x97B425ED, 37, // 54
            0x94F2094F, 0x94F2094F, 37, // 55
            0x49249249, 0x49249249, 36, // 56
            0x47DC11F7, 0x47DC11F7, 36, // 57
            0x8D3DCB09, 0x00000000, 37, // 58
            0x22B63CBF, 0x00000000, 35, // 59
            0x88888889, 0x00000000, 37, // 60
            0x4325C53F, 0x00000000, 36, // 61
            0x42108421, 0x42108421, 36, // 62
            0x41041041, 0x41041041, 36, // 63
            0x00000001, 0x00000000, 6,  // 64
            0xFC0FC0FD, 0x00000000, 38, // 65
            0x3E0F83E1, 0x00000000, 36, // 66
            0x07A44C6B, 0x00000000, 33, // 67
            0xF0F0F0F1, 0x00000000, 38, // 68
            0x76B981DB, 0x00000000, 37, // 69
            0x75075075, 0x75075075, 37, // 70
            0xE6C2B449, 0x00000000, 38, // 71
            0x38E38E39, 0x00000000, 36, // 72
            0x381C0E07, 0x381C0E07, 36, // 73
            0x6EB3E453, 0x6EB3E453, 37, // 74
            0x1B4E81B5, 0x00000000, 35, // 75
            0xD79435E5, 0xD79435E5, 38, // 76
            0x3531DEC1, 0x00000000, 36, // 77
            0x69069069, 0x69069069, 37, // 78
            0xCF6474A9, 0x00000000, 38, // 79
            0xCCCCCCCD, 0x00000000, 38, // 80
            0xCA4587E7, 0x00000000, 38, // 81
            0xC7CE0C7D, 0x00000000, 38, // 82
            0x3159721F, 0x00000000, 36, // 83
            0xC30C30C3, 0xC30C30C3, 38, // 84
            0xC0C0C0C1, 0x00000000, 38, // 85
            0x2FA0BE83, 0x00000000, 36, // 86
            0x2F149903, 0x00000000, 36, // 87
            0xBA2E8BA3, 0x00000000, 38, // 88
            0xB81702E1, 0x00000000, 38, // 89
            0x5B05B05B, 0x5B05B05B, 37, // 90
            0x2D02D02D, 0x2D02D02D, 36, // 91
            0xB21642C9, 0x00000000, 38, // 92
            0xB02C0B03, 0x00000000, 38, // 93
            0xAE4C415D, 0x00000000, 38, // 94
            0x2B1DA461, 0x2B1DA461, 36, // 95
            0xAAAAAAAB, 0x00000000, 38, // 96
            0xA8E83F57, 0xA8E83F57, 38, // 97
            0x5397829D, 0x00000000, 37, // 98
            0xA57EB503, 0x00000000, 38, // 99
            0x51EB851F, 0x00000000, 37, // 100
            0xA237C32B, 0xA237C32B, 38, // 101
            0xA0A0A0A1, 0x00000000, 38, // 102
            0x9F1165E7, 0x9F1165E7, 38, // 103
            0x4EC4EC4F, 0x00000000, 37, // 104
            0x27027027, 0x27027027, 36, // 105
            0x9A90E7D9, 0x9A90E7D9, 38, // 106
            0x991F1A51, 0x991F1A51, 38, // 107
            0x97B425ED, 0x97B425ED, 38, // 108
            0x2593F69B, 0x2593F69B, 36, // 109
            0x94F2094F, 0x94F2094F, 38, // 110
            0x24E6A171, 0x24E6A171, 36, // 111
            0x49249249, 0x49249249, 37, // 112
            0x90FDBC09, 0x90FDBC09, 38, // 113
            0x47DC11F7, 0x47DC11F7, 37, // 114
            0x8E78356D, 0x8E78356D, 38, // 115
            0x8D3DCB09, 0x00000000, 38, // 116
            0x23023023, 0x23023023, 36, // 117
            0x22B63CBF, 0x00000000, 36, // 118
            0x44D72045, 0x00000000, 37, // 119
            0x88888889, 0x00000000, 38, // 120
            0x8767AB5F, 0x8767AB5F, 38, // 121
            0x4325C53F, 0x00000000, 37, // 122
            0x85340853, 0x85340853, 38, // 123
            0x42108421, 0x42108421, 37, // 124
            0x10624DD3, 0x00000000, 35, // 125
            0x41041041, 0x41041041, 37, // 126
            0x10204081, 0x10204081, 35, // 127
            0x00000001, 0x00000000, 7,  // 128
            0x0FE03F81, 0x00000000, 35, // 129
            0xFC0FC0FD, 0x00000000, 39, // 130
            0xFA232CF3, 0x00000000, 39, // 131
            0x3E0F83E1, 0x00000000, 37, // 132
            0xF6603D99, 0x00000000, 39, // 133
            0x07A44C6B, 0x00000000, 34, // 134
            0xF2B9D649, 0x00000000, 39, // 135
            0xF0F0F0F1, 0x00000000, 39, // 136
            0x077975B9, 0x00000000, 34, // 137
            0x76B981DB, 0x00000000, 38, // 138
            0x75DED953, 0x00000000, 38, // 139
            0x75075075, 0x75075075, 38, // 140
            0x3A196B1F, 0x00000000, 37, // 141
            0xE6C2B449, 0x00000000, 39, // 142
            0xE525982B, 0x00000000, 39, // 143
            0x38E38E39, 0x00000000, 37, // 144
            0xE1FC780F, 0x00000000, 39, // 145
            0x381C0E07, 0x381C0E07, 37, // 146
            0xDEE95C4D, 0x00000000, 39, // 147
            0x6EB3E453, 0x6EB3E453, 38, // 148
            0xDBEB61EF, 0x00000000, 39, // 149
            0x1B4E81B5, 0x00000000, 36, // 150
            0x36406C81, 0x00000000, 37, // 151
            0xD79435E5, 0xD79435E5, 39, // 152
            0xD62B80D7, 0x00000000, 39, // 153
            0x3531DEC1, 0x00000000, 37, // 154
            0xD3680D37, 0x00000000, 39, // 155
            0x69069069, 0x69069069, 38, // 156
            0x342DA7F3, 0x00000000, 37, // 157
            0xCF6474A9, 0x00000000, 39, // 158
            0xCE168A77, 0xCE168A77, 39, // 159
            0xCCCCCCCD, 0x00000000, 39, // 160
            0xCB8727C1, 0x00000000, 39, // 161
            0xCA4587E7, 0x00000000, 39, // 162
            0xC907DA4F, 0x00000000, 39, // 163
            0xC7CE0C7D, 0x00000000, 39, // 164
            0x634C0635, 0x00000000, 38, // 165
            0x3159721F, 0x00000000, 37, // 166
            0x621B97C3, 0x00000000, 38, // 167
            0xC30C30C3, 0xC30C30C3, 39, // 168
            0x60F25DEB, 0x00000000, 38, // 169
            0xC0C0C0C1, 0x00000000, 39, // 170
            0x17F405FD, 0x17F405FD, 36, // 171
            0x2FA0BE83, 0x00000000, 37, // 172
            0xBD691047, 0xBD691047, 39, // 173
            0x2F149903, 0x00000000, 37, // 174
            0x5D9F7391, 0x00000000, 38, // 175
            0xBA2E8BA3, 0x00000000, 39, // 176
            0x5C90A1FD, 0x5C90A1FD, 38, // 177
            0xB81702E1, 0x00000000, 39, // 178
            0x5B87DDAD, 0x5B87DDAD, 38, // 179
            0x5B05B05B, 0x5B05B05B, 38, // 180
            0xB509E68B, 0x00000000, 39, // 181
            0x2D02D02D, 0x2D02D02D, 37, // 182
            0xB30F6353, 0x00000000, 39, // 183
            0xB21642C9, 0x00000000, 39, // 184
            0x1623FA77, 0x1623FA77, 36, // 185
            0xB02C0B03, 0x00000000, 39, // 186
            0xAF3ADDC7, 0x00000000, 39, // 187
            0xAE4C415D, 0x00000000, 39, // 188
            0x15AC056B, 0x15AC056B, 36, // 189
            0x2B1DA461, 0x2B1DA461, 37, // 190
            0xAB8F69E3, 0x00000000, 39, // 191
            0xAAAAAAAB, 0x00000000, 39, // 192
            0x15390949, 0x00000000, 36, // 193
            0xA8E83F57, 0xA8E83F57, 39, // 194
            0x15015015, 0x15015015, 36, // 195
            0x5397829D, 0x00000000, 38, // 196
            0xA655C439, 0xA655C439, 39, // 197
            0xA57EB503, 0x00000000, 39, // 198
            0x5254E78F, 0x00000000, 38, // 199
            0x51EB851F, 0x00000000, 38, // 200
            0x028C1979, 0x00000000, 33, // 201
            0xA237C32B, 0xA237C32B, 39, // 202
            0xA16B312F, 0x00000000, 39, // 203
            0xA0A0A0A1, 0x00000000, 39, // 204
            0x4FEC04FF, 0x00000000, 38, // 205
            0x9F1165E7, 0x9F1165E7, 39, // 206
            0x27932B49, 0x00000000, 37, // 207
            0x4EC4EC4F, 0x00000000, 38, // 208
            0x9CC8E161, 0x00000000, 39, // 209
            0x27027027, 0x27027027, 37, // 210
            0x9B4C6F9F, 0x00000000, 39, // 211
            0x9A90E7D9, 0x9A90E7D9, 39, // 212
            0x99D722DB, 0x00000000, 39, // 213
            0x991F1A51, 0x991F1A51, 39, // 214
            0x4C346405, 0x00000000, 38, // 215
            0x97B425ED, 0x97B425ED, 39, // 216
            0x4B809701, 0x4B809701, 38, // 217
            0x2593F69B, 0x2593F69B, 37, // 218
            0x12B404AD, 0x12B404AD, 36, // 219
            0x94F2094F, 0x94F2094F, 39, // 220
            0x25116025, 0x25116025, 37, // 221
            0x24E6A171, 0x24E6A171, 37, // 222
            0x24BC44E1, 0x24BC44E1, 37, // 223
            0x49249249, 0x49249249, 38, // 224
            0x91A2B3C5, 0x00000000, 39, // 225
            0x90FDBC09, 0x90FDBC09, 39, // 226
            0x905A3863, 0x905A3863, 39, // 227
            0x47DC11F7, 0x47DC11F7, 38, // 228
            0x478BBCED, 0x00000000, 38, // 229
            0x8E78356D, 0x8E78356D, 39, // 230
            0x46ED2901, 0x46ED2901, 38, // 231
            0x8D3DCB09, 0x00000000, 39, // 232
            0x2328A701, 0x2328A701, 37, // 233
            0x23023023, 0x23023023, 37, // 234
            0x45B81A25, 0x45B81A25, 38, // 235
            0x22B63CBF, 0x00000000, 37, // 236
            0x08A42F87, 0x08A42F87, 35, // 237
            0x44D72045, 0x00000000, 38, // 238
            0x891AC73B, 0x00000000, 39, // 239
            0x88888889, 0x00000000, 39, // 240
            0x10FEF011, 0x00000000, 36, // 241
            0x8767AB5F, 0x8767AB5F, 39, // 242
            0x86D90545, 0x00000000, 39, // 243
            0x4325C53F, 0x00000000, 38, // 244
            0x85BF3761, 0x85BF3761, 39, // 245
            0x85340853, 0x85340853, 39, // 246
            0x10953F39, 0x10953F39, 36, // 247
            0x42108421, 0x42108421, 38, // 248
            0x41CC9829, 0x41CC9829, 38, // 249
            0x10624DD3, 0x00000000, 36, // 250
            0x828CBFBF, 0x00000000, 39, // 251
            0x41041041, 0x41041041, 38, // 252
            0x81848DA9, 0x00000000, 39, // 253
            0x10204081, 0x10204081, 36, // 254
            0x80808081, 0x00000000, 39  // 255
        };

        #endregion

        /// <summary>
        /// Returns the Distance between two points
        /// </summary>
        public static float Distance(PointF a, PointF b)
        {
            return Magnitude(new PointF(a.X - b.X, a.Y - b.Y));
        }

        /// <summary>
        /// Returns the Magnitude (distance to origin) of a point
        /// </summary>
        public static float Magnitude(PointF p)
        {
            return (float)Math.Sqrt(p.X * p.X + p.Y * p.Y);
        }

        public static float GetAngleOfTransform(Matrix matrix)
        {
            PointF[] pts = new PointF[] { new PointF(1.0f, 0.0f) };
            matrix.TransformVectors(pts);
            double atan2 = Math.Atan2(pts[0].Y, pts[0].X);
            double angle = atan2 * (180.0f / Math.PI);

            return (float)angle;
        }

        public static PointF NormalizeVector(PointF vecF)
        {
            float magnitude = Magnitude(vecF);
            vecF.X /= magnitude;
            vecF.Y /= magnitude;
            return vecF;
        }

        public static PointF NormalizeVector2(PointF vecF)
        {
            float magnitude = Magnitude(vecF);

            if (magnitude == 0)
            {
                vecF.X = 0;
                vecF.Y = 0;
            }
            else
            {
                vecF.X /= magnitude;
                vecF.Y /= magnitude;
            }

            return vecF;
        }

        public static void NormalizeVectors(PointF[] vecsF)
        {
            for (int i = 0; i < vecsF.Length; ++i)
            {
                vecsF[i] = NormalizeVector(vecsF[i]);
            }
        }

        public static PointF RotateVector(PointF vecF, float angleDelta)
        {
            angleDelta *= (float)(Math.PI / 180.0);
            float vecFLen = Magnitude(vecF);
            float vecFAngle = angleDelta + (float)Math.Atan2(vecF.Y, vecF.X);
            vecF.X = (float)Math.Cos(vecFAngle);
            vecF.Y = (float)Math.Sin(vecFAngle);
            return vecF;
        }

        public static void RotateVectors(PointF[] vecFs, float angleDelta)
        {
            for (int i = 0; i < vecFs.Length; ++i)
            {
                vecFs[i] = RotateVector(vecFs[i], angleDelta);
            }
        }

        public static PointF MultiplyVector(PointF vecF, float scalar)
        {
            return new PointF(vecF.X * scalar, vecF.Y * scalar);
        }

        public static PointF AddVectors(PointF a, PointF b)
        {
            return new PointF(a.X + b.X, a.Y + b.Y);
        }

        public static PointF TransformOnePoint(Matrix matrix, PointF ptF)
        {
            PointF[] ptFs = new PointF[1] { ptF };
            matrix.TransformPoints(ptFs);
            return ptFs[0];
        }

        public static Point[] GetLinePoints(Point first, Point second)
        {
            Point[] coords = null;

            int x1 = first.X;
            int y1 = first.Y;
            int x2 = second.X;
            int y2 = second.Y;
            int dx = x2 - x1;
            int dy = y2 - y1;
            int dxabs = Math.Abs(dx);
            int dyabs = Math.Abs(dy);
            int px = x1;
            int py = y1;
            int sdx = Math.Sign(dx);
            int sdy = Math.Sign(dy);
            int x = 0;
            int y = 0;

            if (dxabs > dyabs)
            {
                coords = new Point[dxabs + 1];

                for (int i = 0; i <= dxabs; i++)
                {
                    y += dyabs;

                    if (y >= dxabs)
                    {
                        y -= dxabs;
                        py += sdy;
                    }

                    coords[i] = new Point(px, py);
                    px += sdx;
                }
            }
            else
                // had to add in this cludge for slopes of 1 ... wasn't drawing half the line
                if (dxabs == dyabs)
                {
                    coords = new Point[dxabs + 1];

                    for (int i = 0; i <= dxabs; i++)
                    {
                        coords[i] = new Point(px, py);
                        px += sdx;
                        py += sdy;
                    }
                }
                else
                {
                    coords = new Point[dyabs + 1];

                    for (int i = 0; i <= dyabs; i++)
                    {
                        x += dxabs;

                        if (x >= dyabs)
                        {
                            x -= dyabs;
                            px += sdx;
                        }

                        coords[i] = new Point(px, py);
                        py += sdy;
                    }
                }

            return coords;
        }

        public static Rectangle PointsToRectangle(Point a, Point b)
        {
            int x = Math.Min(a.X, b.X);
            int y = Math.Min(a.Y, b.Y);
            int width = Math.Abs(a.X - b.X) + 1;
            int height = Math.Abs(a.Y - b.Y) + 1;

            return new Rectangle(x, y, width, height);
        }

        public static RectangleF PointsToRectangle(PointF a, PointF b)
        {
            float x = Math.Min(a.X, b.X);
            float y = Math.Min(a.Y, b.Y);
            float width = Math.Abs(a.X - b.X) + 1;
            float height = Math.Abs(a.Y - b.Y) + 1;

            return new RectangleF(x, y, width, height);
        }
    }
}