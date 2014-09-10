using System;
using Photo.Net.Base;
using Photo.Net.Core.Color;

namespace Photo.Net.Core.PixelOperation
{
    /// <summary>
    /// Provides a set of standard BinaryPixelOps.
    /// </summary>
    public sealed class BinaryPixelOps
    {
        private BinaryPixelOps()
        {
        }

        // This is provided solely for data file format compatibility
        [Obsolete("User UserBlendOps.NormalBlendOp instead", true)]
        [Serializable]
        public class AlphaBlend
            : BinaryPixelOp
        {
            public override ColorBgra Apply(ColorBgra lhs, ColorBgra rhs)
            {
                return lhs;
            }
        }

        /// <summary>
        /// F(lhs, rhs) = rhs.A + lhs.R,g,b
        /// </summary>
        public class SetAlphaChannel
            : BinaryPixelOp
        {
            public override ColorBgra Apply(ColorBgra lhs, ColorBgra rhs)
            {
                lhs.A = rhs.A;
                return lhs;
            }
        }

        /// <summary>
        /// F(lhs, rhs) = lhs.R,g,b + rhs.A
        /// </summary>
        public class SetColorChannels
            : BinaryPixelOp
        {
            public override ColorBgra Apply(ColorBgra lhs, ColorBgra rhs)
            {
                rhs.A = lhs.A;
                return rhs;
            }
        }

        /// <summary>
        /// result(lhs,rhs) = rhs
        /// </summary>
        [Serializable]
        public class AssignFromRhs
            : BinaryPixelOp
        {
            public override ColorBgra Apply(ColorBgra lhs, ColorBgra rhs)
            {
                return rhs;
            }

            public unsafe override void Apply(ColorBgra* dst, ColorBgra* lhs, ColorBgra* rhs, int length)
            {
                Memory.Copy(dst, rhs, (ulong)length * ColorBgra.Size);
            }

            public unsafe override void Apply(ColorBgra* dst, ColorBgra* src, int length)
            {
                Memory.Copy(dst, src, (ulong)length * ColorBgra.Size);
            }

            public AssignFromRhs()
            {
            }
        }

        /// <summary>
        /// result(lhs,rhs) = lhs
        /// </summary>
        [Serializable]
        public class AssignFromLhs
            : BinaryPixelOp
        {
            public override ColorBgra Apply(ColorBgra lhs, ColorBgra rhs)
            {
                return lhs;
            }

            public AssignFromLhs()
            {
            }
        }

        [Serializable]
        public class Swap
            : BinaryPixelOp
        {
            BinaryPixelOp swapMyArgs;

            public override ColorBgra Apply(ColorBgra lhs, ColorBgra rhs)
            {
                return swapMyArgs.Apply(rhs, lhs);
            }

            public Swap(BinaryPixelOp swapMyArgs)
            {
                this.swapMyArgs = swapMyArgs;
            }
        }
    }
}
