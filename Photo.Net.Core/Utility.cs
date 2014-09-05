using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Photo.Net.Core.Geometry;
using Photo.Net.Core.Struct;
using Color = System.Drawing.Color;

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
    }
}