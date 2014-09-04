using System;
using System.Runtime.InteropServices;

namespace Photo.Net.Core.Color
{
    /// <summary>
    /// This represent color be indexed, usually work with IndexedColorTable, and it only have 255 colors.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Explicit)]
    public class IndexedColor : IColor
    {

        [FieldOffset(0)]
        public byte Index;

        public int SizeOf { get { return 1; } }
    }
}
