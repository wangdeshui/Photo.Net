﻿using System;
using System.Drawing;

namespace Photo.Net.Tool.Events
{
    public sealed class MovingEventArgs
        : EventArgs
    {
        public Rectangle Rectangle { get; set; }

        public MovingEventArgs(Rectangle rect)
        {
            this.Rectangle = rect;
        }
    }
}
