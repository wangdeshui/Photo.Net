using System;
using System.Runtime.Serialization;
using Photo.Net.Base;
using Photo.Net.Core.Struct;
using Photo.Net.Tool.Core.Enums;

namespace Photo.Net.Tool.Core
{
    [Serializable]
    internal sealed class SelectionDrawModeInfo
        : ICloneable<SelectionDrawModeInfo>,
          IDeserializationCallback
    {
        private SelectionDrawMode drawMode;
        private double width;
        private double height;
        private MeasurementUnit units;

        public SelectionDrawMode DrawMode
        {
            get
            {
                return this.drawMode;
            }
        }

        public double Width
        {
            get
            {
                return this.width;
            }
        }

        public double Height
        {
            get
            {
                return this.height;
            }
        }

        public MeasurementUnit Units
        {
            get
            {
                return this.units;
            }
        }

        public override bool Equals(object obj)
        {
            SelectionDrawModeInfo asSDMI = obj as SelectionDrawModeInfo;

            if (asSDMI == null)
            {
                return false;
            }

            return (asSDMI.drawMode == this.drawMode) && (asSDMI.width == this.width) && (asSDMI.height == this.height) && (asSDMI.units == this.units);
        }

        public override int GetHashCode()
        {
            return unchecked(this.drawMode.GetHashCode() ^ this.width.GetHashCode() ^ this.height.GetHashCode() & this.units.GetHashCode());
        }

        public SelectionDrawModeInfo(SelectionDrawMode drawMode, double width, double height, MeasurementUnit units)
        {
            this.drawMode = drawMode;
            this.width = width;
            this.height = height;
            this.units = units;
        }

        public static SelectionDrawModeInfo CreateDefault()
        {
            return new SelectionDrawModeInfo(SelectionDrawMode.Normal, 4.0, 3.0, MeasurementUnit.Inch);
        }

        public SelectionDrawModeInfo CloneWithNewDrawMode(SelectionDrawMode newDrawMode)
        {
            return new SelectionDrawModeInfo(newDrawMode, this.width, this.height, this.units);
        }

        public SelectionDrawModeInfo CloneWithNewWidth(double newWidth)
        {
            return new SelectionDrawModeInfo(this.drawMode, newWidth, this.height, this.units);
        }

        public SelectionDrawModeInfo CloneWithNewHeight(double newHeight)
        {
            return new SelectionDrawModeInfo(this.drawMode, this.width, newHeight, this.units);
        }

        public SelectionDrawModeInfo CloneWithNewWidthAndHeight(double newWidth, double newHeight)
        {
            return new SelectionDrawModeInfo(this.drawMode, newWidth, newHeight, this.units);
        }

        public SelectionDrawModeInfo Clone()
        {
            return new SelectionDrawModeInfo(this.drawMode, this.width, this.height, this.units);
        }

        public SelectionDrawModeInfo CloneWithNewUnits(MeasurementUnit newUnits)
        {
            return new SelectionDrawModeInfo(this.drawMode, this.width, this.height, newUnits);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        void IDeserializationCallback.OnDeserialization(object sender)
        {
            switch (this.units)
            {
                case MeasurementUnit.Centimeter:
                case MeasurementUnit.Inch:
                case MeasurementUnit.Pixel:
                    break;

                default:
                    this.units = MeasurementUnit.Pixel;
                    break;
            }
        }
    }
}
