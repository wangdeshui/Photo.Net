using System;

namespace Photo.Net.Tool.Core.Enums
{
    [Flags]
    public enum ToolBarConfigItems
        : uint
    {
        None = 0,
        All = ~None,

        // IMPORTANT: Keep these in alphabetical order.
        AlphaBlending = 1,
        Antialiasing = 2,
        Brush = 4,
        ColorPickerBehavior = 8,
        FloodMode = 4096,
        Gradient = 16,
        Pen = 32,
        PenCaps = 64,
        SelectionCombineMode = 2048,
        SelectionDrawMode = 8192,
        ShapeType = 128,
        Resampling = 256,
        Text = 512,
        Tolerance = 1024,
    }
}
