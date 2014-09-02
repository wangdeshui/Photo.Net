using System;

namespace Photo.Net.Base.Infomation
{
    /// <summary>
    /// Identify the current CPU instruction set
    /// </summary>
    [Flags]
    public enum ProcessorFeatures
    {
        None,
        DEP = 1,
        SSE = 2,
        SSE2 = 4,
        SSE3 = 8,
    }
}
