using System;

namespace Photo.Net.Tool.IO
{
    [Flags]
    public enum FileTypeFlags
        : long
    {
        None = 0,
        SupportsLayers = 1,
        SupportsCustomHeaders = 2,
        SupportsSaving = 4,
        SupportsLoading = 8,
        SavesWithProgress = 16
    }
}

