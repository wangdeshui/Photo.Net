using System.Drawing;
using Photo.Net.Core;
using Photo.Net.Core.Color;

namespace Photo.Net.Gdi.Surfaces
{
    /// <summary>
    /// This class implement the base render of Surfacebox control.
    /// </summary>
    public sealed class SurfaceBoxBaseRenderer
        : SurfaceBoxRenderer
    {
        private Surface _source;
        private RenderDelegate _renderDelegate;

        public Surface Source
        {
            get
            {
                return this._source;
            }

            set
            {
                this._source = value;
                Flush();
            }
        }

        private void Flush()
        {
            this._renderDelegate = null;
        }

        protected override void OnVisibleChanged()
        {
            Invalidate();
        }

        /// <summary>
        /// Choose a render method at diffrent zoom scale.
        /// </summary>
        private void ChooseRenderDelegate()
        {
            if (SourceSize.Width > DestinationSize.Width)
            {
                // zoom out
                this._renderDelegate = RenderZoomOutRotatedGridMultisampling;
            }
            else if (SourceSize == DestinationSize)
            {
                // zoom 100%
                this._renderDelegate = RenderOneToOne;
            }
            else if (SourceSize.Width < DestinationSize.Width)
            {
                // zoom in
                this._renderDelegate = RenderZoomInNearestNeighbor;
            }
        }

        public override void OnDestinationSizeChanged()
        {
            ChooseRenderDelegate();
            this.OwnerList.InvalidateLookups();
            base.OnDestinationSizeChanged();
        }

        public override void OnSourceSizeChanged()
        {
            ChooseRenderDelegate();
            this.OwnerList.InvalidateLookups();
            base.OnSourceSizeChanged();
        }

        public static void RenderOneToOne(Surface dst, Surface source, Point offset)
        {
            unsafe
            {
                var srcRect = new Rectangle(offset, dst.Size);
                srcRect.Intersect(source.Bounds);

                for (int dstRow = 0; dstRow < srcRect.Height; ++dstRow)
                {
                    ColorBgra* dstRowPtr = dst.UnsafeGetRowAddress(dstRow);
                    ColorBgra* srcRowPtr = source.UnsafeGetPointAddress(offset.X, dstRow + offset.Y);

                    int dstCol = offset.X;
                    int dstColEnd = offset.X + srcRect.Width;
                    int checkerY = dstRow + offset.Y;

                    while (dstCol < dstColEnd)
                    {
                        int b = srcRowPtr->B;
                        int g = srcRowPtr->G;
                        int r = srcRowPtr->R;
                        int a = srcRowPtr->A;

                        // Blend it over the checkerboard background
                        int v = (((dstCol ^ checkerY) & 8) << 3) + 191;
                        a = a + (a >> 7);
                        int vmia = v * (256 - a);

                        r = ((r * a) + vmia) >> 8;
                        g = ((g * a) + vmia) >> 8;
                        b = ((b * a) + vmia) >> 8;

                        dstRowPtr->Bgra = (uint)b + ((uint)g << 8) + ((uint)r << 16) + ((uint)255 << 24);
                        ++dstRowPtr;
                        ++srcRowPtr;
                        ++dstCol;
                    }
                }
            }
        }

        private void RenderOneToOne(Surface dst, Point offset)
        {
            RenderOneToOne(dst, this._source, offset);
        }

        private void RenderZoomInNearestNeighbor(Surface dst, Point offset)
        {
            unsafe
            {
                int[] d2SLookupY = OwnerList.Dst2SrcLookupY;
                int[] d2SLookupX = OwnerList.Dst2SrcLookupX;

                for (int dstRow = 0; dstRow < dst.Height; ++dstRow)
                {
                    int nnY = dstRow + offset.Y;
                    int srcY = d2SLookupY[nnY];
                    ColorBgra* dstPtr = dst.UnsafeGetRowAddress(dstRow);
                    ColorBgra* srcRow = this._source.UnsafeGetRowAddress(srcY);

                    for (int dstCol = 0; dstCol < dst.Width; ++dstCol)
                    {
                        int nnX = dstCol + offset.X;
                        int srcX = d2SLookupX[nnX];

                        ColorBgra src = *(srcRow + srcX);
                        int b = src.B;
                        int g = src.G;
                        int r = src.R;
                        int a = src.A;

                        // Blend it over the checkerboard background
                        int v = (((dstCol + offset.X) ^ (dstRow + offset.Y)) & 8) * 8 + 191;
                        a = a + (a >> 7);
                        int vmia = v * (256 - a);

                        r = ((r * a) + vmia) >> 8;
                        g = ((g * a) + vmia) >> 8;
                        b = ((b * a) + vmia) >> 8;

                        dstPtr->Bgra = (uint)b + ((uint)g << 8) + ((uint)r << 16) + ((uint)255 << 24);

                        ++dstPtr;
                    }
                }
            }
        }

        public static void RenderZoomOutRotatedGridMultisampling(Surface dst, Surface source, Point offset, Size destinationSize)
        {
            unsafe
            {
                const int fpShift = 12;
                const int fpFactor = (1 << fpShift);

                Size sourceSize = source.Size;
                long fDstLeftLong = ((long)offset.X * fpFactor * (long)sourceSize.Width) / (long)destinationSize.Width;
                long fDstTopLong = ((long)offset.Y * fpFactor * (long)sourceSize.Height) / (long)destinationSize.Height;
                long fDstRightLong = ((long)(offset.X + dst.Width) * fpFactor * (long)sourceSize.Width) / (long)destinationSize.Width;
                long fDstBottomLong = ((long)(offset.Y + dst.Height) * fpFactor * (long)sourceSize.Height) / (long)destinationSize.Height;
                int fDstLeft = (int)fDstLeftLong;
                int fDstTop = (int)fDstTopLong;
                int fDstRight = (int)fDstRightLong;
                int fDstBottom = (int)fDstBottomLong;
                int dx = (fDstRight - fDstLeft) / dst.Width;
                int dy = (fDstBottom - fDstTop) / dst.Height;

                for (int dstRow = 0, fDstY = fDstTop;
                    dstRow < dst.Height && fDstY < fDstBottom;
                    ++dstRow, fDstY += dy)
                {
                    int srcY1 = fDstY >> fpShift;                            // y
                    int srcY2 = (fDstY + (dy >> 2)) >> fpShift;              // y + 0.25
                    int srcY3 = (fDstY + (dy >> 1)) >> fpShift;              // y + 0.50
                    int srcY4 = (fDstY + (dy >> 1) + (dy >> 2)) >> fpShift;  // y + 0.75

                    ColorBgra* src1 = source.UnsafeGetRowAddress(srcY1);
                    ColorBgra* src2 = source.UnsafeGetRowAddress(srcY2);
                    ColorBgra* src3 = source.UnsafeGetRowAddress(srcY3);
                    ColorBgra* src4 = source.UnsafeGetRowAddress(srcY4);
                    ColorBgra* dstPtr = dst.UnsafeGetRowAddress(dstRow);
                    int checkerY = dstRow + offset.Y;
                    int checkerX = offset.X;
                    int maxCheckerX = checkerX + dst.Width;

                    for (int fDstX = fDstLeft;
                         checkerX < maxCheckerX && fDstX < fDstRight;
                         ++checkerX, fDstX += dx)
                    {
                        int srcX1 = (fDstX + (dx >> 2)) >> fpShift;             // x + 0.25
                        int srcX2 = (fDstX + (dx >> 1) + (dx >> 2)) >> fpShift; // x + 0.75
                        int srcX3 = fDstX >> fpShift;                           // x
                        int srcX4 = (fDstX + (dx >> 1)) >> fpShift;             // x + 0.50

                        ColorBgra* p1 = src1 + srcX1;
                        ColorBgra* p2 = src2 + srcX2;
                        ColorBgra* p3 = src3 + srcX3;
                        ColorBgra* p4 = src4 + srcX4;

                        int r = (2 + p1->R + p2->R + p3->R + p4->R) >> 2;
                        int g = (2 + p1->G + p2->G + p3->G + p4->G) >> 2;
                        int b = (2 + p1->B + p2->B + p3->B + p4->B) >> 2;
                        int a = (2 + p1->A + p2->A + p3->A + p4->A) >> 2;

                        // Blend it over the checkerboard background
                        int v = ((checkerX ^ checkerY) & 8) * 8 + 191;
                        a = a + (a >> 7);
                        int vmia = v * (256 - a);

                        r = ((r * a) + vmia) >> 8;
                        g = ((g * a) + vmia) >> 8;
                        b = ((b * a) + vmia) >> 8;

                        dstPtr->Bgra = (uint)b + ((uint)g << 8) + ((uint)r << 16) + 0xff000000;
                        ++dstPtr;
                    }
                }
            }
        }

        private void RenderZoomOutRotatedGridMultisampling(Surface dst, Point offset)
        {
            RenderZoomOutRotatedGridMultisampling(dst, this._source, offset, this.DestinationSize);
        }

        public override void Render(Surface dst, Point offset)
        {
            if (this._renderDelegate == null)
            {
                ChooseRenderDelegate();
            }

            var renderDelegate = this._renderDelegate;
            if (renderDelegate != null) renderDelegate(dst, offset);
        }

        public SurfaceBoxBaseRenderer(SurfaceBoxRenderList ownerList, Surface source)
            : base(ownerList)
        {
            this._source = source;
            ChooseRenderDelegate();
        }
    }
}
