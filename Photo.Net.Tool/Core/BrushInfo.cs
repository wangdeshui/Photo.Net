﻿using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Photo.Net.Tool.Core.Enums;

namespace Photo.Net.Tool.Core
{
    /// <summary>
    /// Carries information about the subset of Brush configuration details that we support.
    /// Does not carry color information.
    /// </summary>
    [Serializable]
    public class BrushInfo
        : ICloneable
    {
        private BrushType brushType;
        private HatchStyle hatchStyle;

        public BrushType BrushType
        {
            get
            {
                return brushType;
            }

            set
            {
                brushType = value;
            }
        }

        /// <summary>
        /// If BrushType is equal to BrushType.Hatch, then this info is pertinent.
        /// </summary>
        public HatchStyle HatchStyle
        {
            get
            {
                return hatchStyle;
            }

            set
            {
                hatchStyle = value;
            }
        }

        public Brush CreateBrush(Color foreColor, Color backColor)
        {
            if (brushType == BrushType.Solid)
            {
                return new SolidBrush(foreColor);
            }
            else if (brushType == BrushType.Hatch)
            {
                return new HatchBrush(hatchStyle, foreColor, backColor);
            }

            throw new InvalidOperationException("BrushType is invalid");
        }

        public BrushInfo(BrushType brushType, HatchStyle hatchStyle)
        {
            this.brushType = brushType;
            this.hatchStyle = hatchStyle;
        }

        public BrushInfo Clone()
        {
            return new BrushInfo(this.brushType, this.hatchStyle);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}
