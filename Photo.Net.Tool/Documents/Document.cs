﻿using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using Photo.Net.Base;
using Photo.Net.Base.Delegate;
using Photo.Net.Base.Enums;
using Photo.Net.Base.Infomation;
using Photo.Net.Base.IO;
using Photo.Net.Base.Serializable;
using Photo.Net.Base.Serialization;
using Photo.Net.Core;
using Photo.Net.Core.Area;
using Photo.Net.Core.Color;
using Photo.Net.Core.PixelOperation;
using Photo.Net.Core.Struct;
using Photo.Net.Gdi.Event;
using Photo.Net.Tool.Images;
using Photo.Net.Tool.Layer;
using ThreadPool = Photo.Net.Base.Thread.ThreadPool;

namespace Photo.Net.Tool.Documents
{
    [Serializable]
    public sealed class Document
        : IDeserializationCallback,
          IDisposable,
          ICloneable
    {
        #region Fields

        private readonly ImageLayerList _layers;
        private readonly int _width;
        private readonly int _height;
        private readonly NameValueCollection _userMetaData;

        [NonSerialized]
        private ThreadPool _threadPool = new ThreadPool();

        [NonSerialized]
        private InvalidateEventHandler _layerInvalidatedDelegate;

        [NonSerialized]
        private Vector<Rectangle> _updateRegion;

        [NonSerialized]
        private bool _dirty;

        private Version _savedWith;

        #endregion

        #region Unit

        /// <summary>
        /// Gets or sets the units that are used for measuring the document's physical (printed) size.
        /// </summary>
        /// <remarks>
        /// If this property is set to MeasurementUnit.Pixel, then Dpu will be reset to 1. 
        /// If this property has not been set in the image's metadata, its default value 
        /// will be MeasurementUnit.Inch.
        /// If the EXIF data for the image is invalid (such as "ResolutionUnit = 0" or something),
        /// then the default DpuUnit will be returned.
        /// </remarks>
        public MeasurementUnit DpuUnit
        {
            get
            {
                PropertyItem[] pis = this.Metadata.GetExifValues(ExifTagID.ResolutionUnit);

                if (pis.Length == 0)
                {
                    this.DpuUnit = DefaultDpuUnit;
                    return DefaultDpuUnit;
                }
                else
                {
                    try
                    {
                        ushort unit = Exif.DecodeShortValue(pis[0]);

                        // Guard against bad data in the EXIF store
                        switch ((MeasurementUnit)unit)
                        {
                            case MeasurementUnit.Centimeter:
                            case MeasurementUnit.Inch:
                            case MeasurementUnit.Pixel:
                                return (MeasurementUnit)unit;

                            default:
                                this.Metadata.RemoveExifValues(ExifTagID.ResolutionUnit);
                                return this.DpuUnit; // recursive call
                        }
                    }

                    catch (Exception)
                    {
                        this.Metadata.RemoveExifValues(ExifTagID.ResolutionUnit);
                        return this.DpuUnit; // recursive call
                    }
                }
            }

            set
            {
                PropertyItem pi = Exif.CreateShort(ExifTagID.ResolutionUnit, (ushort)value);
                this.Metadata.ReplaceExifValues(ExifTagID.ResolutionUnit, new PropertyItem[1] { pi });

                if (value == MeasurementUnit.Pixel)
                {
                    this.DpuX = 1.0;
                    this.DpuY = 1.0;
                }

                Dirty = true;
            }
        }

        public static MeasurementUnit DefaultDpuUnit
        {
            get { return MeasurementUnit.Inch; }
        }


        public const double DefaultDpi = 96.0;

        public const double CmPerInch = 2.54;
        private const double defaultDpcm = DefaultDpi / CmPerInch;

        public static double DefaultDpcm
        {
            get { return defaultDpcm; }
        }

        public const double MinimumDpu = 0.01;
        public const double MaximumDpu = 32767.0;

        #endregion

        #region Measure

        /// <summary>
        /// Ensures that the document's DpuX, DpuY, and DpuUnits properties are set.
        /// If they are not already set, they are initialized to their default values (96, 96 , inches).
        /// </summary>
        private void InitializeDpu()
        {
            this.DpuUnit = this.DpuUnit;
            this.DpuX = this.DpuX;
            this.DpuY = this.DpuY;
        }

        public static double InchesToCentimeters(double inches)
        {
            return inches * CmPerInch;
        }

        public static double CentimetersToInches(double centimeters)
        {
            return centimeters / CmPerInch;
        }

        public static double DotsPerInchToDotsPerCm(double dpi)
        {
            return dpi / CmPerInch;
        }

        public static double DotsPerCmToDotsPerInch(double dpcm)
        {
            return dpcm * CmPerInch;
        }

        public static double GetDefaultDpu(MeasurementUnit units)
        {
            double dpu;

            switch (units)
            {
                case MeasurementUnit.Inch:
                    dpu = DefaultDpi;
                    break;

                case MeasurementUnit.Centimeter:
                    dpu = defaultDpcm;
                    break;

                case MeasurementUnit.Pixel:
                    dpu = 1.0;
                    break;

                default:
                    throw new InvalidEnumArgumentException("DpuUnit", (int)units, typeof(MeasurementUnit));
            }

            return dpu;
        }

        private byte[] GetDoubleAsRationalExifData(double value)
        {
            uint numerator;
            uint denominator;

            if (Math.IEEERemainder(value, 1.0) == 0)
            {
                numerator = (uint)value;
                denominator = 1;
            }
            else
            {
                double s = value * 1000.0;
                numerator = (uint)Math.Floor(s);
                denominator = 1000;
            }

            return Exif.EncodeRationalValue(numerator, denominator);
        }

        /// <summary>
        /// Gets or sets the Document's dots-per-unit scale in the X direction.
        /// </summary>
        /// <remarks>
        /// If DpuUnit is equal to MeasurementUnit.Pixel, then this property may not be set
        /// to any value other than 1.0. Setting DpuUnit to MeasurementUnit.Pixel will reset
        /// this property to 1.0. This property may only be set to a value greater than 0.
        /// One dot is always equal to one pixel. This property will not return a value less
        /// than MinimumDpu, nor a value larger than MaximumDpu.
        /// </remarks>
        public double DpuX
        {
            get
            {
                PropertyItem[] pis = this.Metadata.GetExifValues(ExifTagID.XResolution);

                if (pis.Length == 0)
                {
                    double defaultDpu = GetDefaultDpu(this.DpuUnit);
                    //                    this.DpuX = defaultDpu;
                    return defaultDpu;
                }
                else
                {
                    try
                    {
                        uint numerator;
                        uint denominator;

                        Exif.DecodeRationalValue(pis[0], out numerator, out denominator);

                        if (denominator == 0)
                        {
                            throw new DivideByZeroException(); // will be caught by the below catch{}
                        }
                        else
                        {
                            return Math.Min(MaximumDpu, Math.Max(MinimumDpu, (double)numerator / (double)denominator));
                        }
                    }

                    catch
                    {
                        this.Metadata.RemoveExifValues(ExifTagID.XResolution);
                        return this.DpuX; // recursive call;
                    }
                }
            }

            set
            {
                if (value <= 0.0)
                {
                    throw new ArgumentOutOfRangeException("value", value, "must be > 0.0");
                }

                if (this.DpuUnit == MeasurementUnit.Pixel && value != 1.0)
                {
                    throw new ArgumentOutOfRangeException("value", value,
                        "if DpuUnit == Pixel, then value must equal 1.0");
                }

                byte[] data = GetDoubleAsRationalExifData(value);

                PropertyItem pi = Exif.CreatePropertyItem(ExifTagID.XResolution, ExifTagType.Rational, data);
                this.Metadata.ReplaceExifValues(ExifTagID.XResolution, new PropertyItem[1] { pi });
                Dirty = true;
            }
        }

        /// <summary>
        /// Gets or sets the Document's dots-per-unit scale in the Y direction.
        /// </summary>
        /// <remarks>
        /// If DpuUnit is equal to MeasurementUnit.Pixel, then this property may not be set
        /// to any value other than 1.0. Setting DpuUnit to MeasurementUnit.Pixel will reset
        /// this property to 1.0. This property may only be set to a value greater than 0.
        /// One dot is always equal to one pixel. This property will not return a value less
        /// than MinimumDpu, nor a value larger than MaximumDpu.
        /// </remarks>
        public double DpuY
        {
            get
            {
                PropertyItem[] pis = this.Metadata.GetExifValues(ExifTagID.YResolution);

                if (pis.Length == 0)
                {
                    // If there's no DpuY setting, default to the DpuX setting
                    double dpu = this.DpuX;
                    this.DpuY = dpu;
                    return dpu;
                }
                else
                {
                    try
                    {
                        uint numerator;
                        uint denominator;

                        Exif.DecodeRationalValue(pis[0], out numerator, out denominator);

                        if (denominator == 0)
                        {
                            throw new DivideByZeroException(); // will be caught by the below catch{}
                        }
                        else
                        {
                            return Math.Min(MaximumDpu, Math.Max(MinimumDpu, (double)numerator / (double)denominator));
                        }
                    }

                    catch
                    {
                        this.Metadata.RemoveExifValues(ExifTagID.YResolution);
                        return this.DpuY; // recursive call;
                    }
                }
            }

            set
            {
                if (value <= 0.0)
                {
                    throw new ArgumentOutOfRangeException("value", value, "must be > 0.0");
                }

                if (this.DpuUnit == MeasurementUnit.Pixel && value != 1.0)
                {
                    throw new ArgumentOutOfRangeException("value", value,
                        "if DpuUnit == Pixel, then value must equal 1.0");
                }

                byte[] data = GetDoubleAsRationalExifData(value);

                PropertyItem pi = Exif.CreatePropertyItem(ExifTagID.YResolution, ExifTagType.Rational, data);
                this.Metadata.ReplaceExifValues(ExifTagID.YResolution, new PropertyItem[1] { pi });
                Dirty = true;
            }
        }

        /// <summary>
        /// Gets the Document's measured physical width based on the DpuUnit and DpuX properties.
        /// </summary>
        public double PhysicalWidth
        {
            get { return (double)this.Width / (double)this.DpuX; }
        }

        /// <summary>
        /// Gets the Document's measured physical height based on the DpuUnit and DpuY properties.
        /// </summary>
        public double PhysicalHeight
        {
            get { return (double)this.Height / (double)this.DpuY; }
        }

        public static double PixelToPhysical(double pixel, MeasurementUnit resultUnit, MeasurementUnit dpuUnit,
            double dpu)
        {
            double result;

            if (resultUnit == MeasurementUnit.Pixel)
            {
                result = pixel;
            }
            else
            {
                if (resultUnit == dpuUnit)
                {
                    result = pixel / dpu;
                }
                else if (dpuUnit == MeasurementUnit.Pixel)
                {
                    double defaultDpu = GetDefaultDpu(dpuUnit);
                    result = pixel / defaultDpu;
                }
                else if (dpuUnit == MeasurementUnit.Centimeter && resultUnit == MeasurementUnit.Inch)
                {
                    result = pixel / (CmPerInch * dpu);
                }
                else // if (dpuUnit == MeasurementUnit.Inch && resultUnit == MeasurementUnit.Centimeter)
                {
                    result = (pixel * CmPerInch) / dpu;
                }
            }

            return result;
        }

        public double PixelToPhysicalX(double pixel, MeasurementUnit resultUnit)
        {
            double result;

            if (resultUnit == MeasurementUnit.Pixel)
            {
                result = pixel;
            }
            else
            {
                MeasurementUnit dpuUnit = this.DpuUnit;

                if (resultUnit == dpuUnit)
                {
                    result = pixel / this.DpuX;
                }
                else if (dpuUnit == MeasurementUnit.Pixel)
                {
                    double defaultDpuX = GetDefaultDpu(dpuUnit);
                    result = pixel / defaultDpuX;
                }
                else if (dpuUnit == MeasurementUnit.Centimeter && resultUnit == MeasurementUnit.Inch)
                {
                    result = pixel / (CmPerInch * this.DpuX);
                }
                else //if (dpuUnit == MeasurementUnit.Inch && resultUnit == MeasurementUnit.Centimeter)
                {
                    result = (pixel * CmPerInch) / this.DpuX;
                }
            }

            return result;
        }

        public double PixelToPhysicalY(double pixel, MeasurementUnit resultUnit)
        {
            double result;

            if (resultUnit == MeasurementUnit.Pixel)
            {
                result = pixel;
            }
            else
            {
                MeasurementUnit dpuUnit = this.DpuUnit;

                if (resultUnit == dpuUnit)
                {
                    result = pixel / this.DpuY;
                }
                else if (dpuUnit == MeasurementUnit.Pixel)
                {
                    double defaultDpuY = GetDefaultDpu(dpuUnit);
                    result = pixel / defaultDpuY;
                }
                else if (dpuUnit == MeasurementUnit.Centimeter && resultUnit == MeasurementUnit.Inch)
                {
                    result = pixel / (CmPerInch * this.DpuY);
                }
                else //if (dpuUnit == MeasurementUnit.Inch && resultUnit == MeasurementUnit.Centimeter)
                {
                    result = (pixel * CmPerInch) / this.DpuY;
                }
            }

            return result;
        }

        private static bool IsValidMeasurementUnit(MeasurementUnit unit)
        {
            switch (unit)
            {
                case MeasurementUnit.Pixel:
                case MeasurementUnit.Centimeter:
                case MeasurementUnit.Inch:
                    return true;

                default:
                    return false;
            }
        }

        public static double ConvertMeasurement(
            double sourceLength,
            MeasurementUnit sourceUnits,
            MeasurementUnit basisDpuUnits,
            double basisDpu,
            MeasurementUnit resultDpuUnits)
        {
            // Validation
            if (!IsValidMeasurementUnit(sourceUnits))
            {
                throw new InvalidEnumArgumentException("sourceUnits", (int)sourceUnits, typeof(MeasurementUnit));
            }

            if (!IsValidMeasurementUnit(basisDpuUnits))
            {
                throw new InvalidEnumArgumentException("basisDpuUnits", (int)basisDpuUnits, typeof(MeasurementUnit));
            }

            if (!IsValidMeasurementUnit(resultDpuUnits))
            {
                throw new InvalidEnumArgumentException("resultDpuUnits", (int)resultDpuUnits, typeof(MeasurementUnit));
            }

            if (basisDpuUnits == MeasurementUnit.Pixel && basisDpu != 1.0)
            {
                throw new ArgumentOutOfRangeException("basisDpuUnits, basisDpu",
                    "if basisDpuUnits is Pixel, then basisDpu must equal 1.0");
            }

            // Case 1. No conversion is necessary if they want the same units out.
            if (sourceUnits == resultDpuUnits)
            {
                return sourceLength;
            }

            // Case 2. Simple inches -> centimeters
            if (sourceUnits == MeasurementUnit.Inch && resultDpuUnits == MeasurementUnit.Centimeter)
            {
                return InchesToCentimeters(sourceLength);
            }

            // Case 3. Simple centimeters -> inches.
            if (sourceUnits == MeasurementUnit.Centimeter && resultDpuUnits == MeasurementUnit.Inch)
            {
                return CentimetersToInches(sourceLength);
            }

            // At this point we know we are converting from non-pixels to pixels, or from pixels
            // to non-pixels. 
            // Cases 4 through 8 cover conversion from non-pixels to pixels. 
            // Cases 9 through 11 cover conversion from pixels to non-pixels.

            // Case 4. Conversion from pixels to inches/centimeters when basis is in pixels too. 
            // This means we must use the default DPU for the desired result measurement.
            // No need to compare lengthUnits != resultDpuUnits, since we already know this to 
            // be true from case 1.
            if (sourceUnits == MeasurementUnit.Pixel && basisDpuUnits == MeasurementUnit.Pixel)
            {
                double dpu = GetDefaultDpu(resultDpuUnits);
                double lengthInOrCm = sourceLength / dpu;
                return lengthInOrCm;
            }

            // Case 5. Conversion from inches/centimeters to pixels when basis is in pixels too.
            // This means we must use the default DPU for the given input measurement.
            if (sourceUnits != MeasurementUnit.Pixel && basisDpuUnits == MeasurementUnit.Pixel)
            {
                double dpu = GetDefaultDpu(sourceUnits);
                double resultPx = sourceLength * dpu;
                return resultPx;
            }

            // Case 6. Conversion from inches/centimeters to pixels, when basis is in same units as input.
            if (sourceUnits == basisDpuUnits && resultDpuUnits == MeasurementUnit.Pixel)
            {
                double resultPx = sourceLength * basisDpu;
                return resultPx;
            }

            // Case 7. Conversion from inches to pixels, when basis is in centimeters.
            if (sourceUnits == MeasurementUnit.Inch && basisDpuUnits == MeasurementUnit.Centimeter)
            {
                double dpi = DotsPerCmToDotsPerInch(basisDpu);
                double resultPx = sourceLength * dpi;
                return resultPx;
            }

            // Case 8. Conversion from centimeters to pixels, when basis is in inches.
            if (sourceUnits == MeasurementUnit.Centimeter && basisDpuUnits == MeasurementUnit.Inch)
            {
                double dpcm = DotsPerInchToDotsPerCm(basisDpu);
                double resultPx = sourceLength * dpcm;
                return resultPx;
            }

            // Case 9. Converting from pixels to inches/centimeters, when the basis and result
            // units are the same.
            if (basisDpuUnits == resultDpuUnits)
            {
                double resultInOrCm = sourceLength / basisDpu;
                return resultInOrCm;
            }

            // Case 10. Converting from pixels to centimeters, when the basis is in inches.
            if (resultDpuUnits == MeasurementUnit.Centimeter && basisDpuUnits == MeasurementUnit.Inch)
            {
                double dpcm = DotsPerInchToDotsPerCm(basisDpu);
                double resultCm = sourceLength / dpcm;
                return resultCm;
            }

            // Case 11. Converting from pixels to inches, when the basis is in centimeters.
            if (resultDpuUnits == MeasurementUnit.Inch && basisDpuUnits == MeasurementUnit.Centimeter)
            {
                double dpi = DotsPerCmToDotsPerInch(basisDpu);
                double resultIn = sourceLength / dpi;
                return resultIn;
            }

            // Should not be possible to get here, but must appease the compiler.
            throw new InvalidOperationException();
        }

        public double PixelAreaToPhysicalArea(double area, MeasurementUnit resultUnit)
        {
            double xScale = PixelToPhysicalX(1.0, resultUnit);
            double yScale = PixelToPhysicalY(1.0, resultUnit);

            return area * xScale * yScale;
        }

        private static string GetUnitsAbbreviation(MeasurementUnit units)
        {
            string result;

            //            switch (units)
            //            {
            //                case MeasurementUnit.Pixel:
            //                    result = string.Empty;
            //                    break;
            //
            //                case MeasurementUnit.Centimeter:
            //                    result = PdnResources.GetString("MeasurementUnit.Centimeter.Abbreviation");
            //                    break;
            //
            //                case MeasurementUnit.Inch:
            //                    result = PdnResources.GetString("MeasurementUnit.Inch.Abbreviation");
            //                    break;
            //
            //                default:
            //                    throw new InvalidEnumArgumentException("MeasurementUnit was invalid");
            //            }

            //            return result;
            return null;
        }

        public void CoordinatesToStrings(MeasurementUnit units, int x, int y, out string xString, out string yString,
            out string unitsString)
        {
            string unitsAbbreviation = GetUnitsAbbreviation(units);

            unitsString = GetUnitsAbbreviation(units);

            if (units == MeasurementUnit.Pixel)
            {
                xString = x.ToString();
                yString = y.ToString();
            }
            else
            {
                double physicalX = PixelToPhysicalX(x, units);
                xString = physicalX.ToString("F2");

                double physicalY = PixelToPhysicalY(y, units);
                yString = physicalY.ToString("F2");
            }
        }

        #endregion

        #region Propertys

        /// <summary>
        /// Exposes a collection for access to the layers, and for manipulation of
        /// the way the document contains the layers (add/remove/move).
        /// </summary>
        public ImageLayerList Layers
        {
            get
            {
                if (disposed)
                {
                    throw new ObjectDisposedException("Document");
                }

                return _layers;
            }
        }

        /// <summary>
        /// Width of the document, in pixels. All contained layers must be this wide as well.
        /// </summary>
        public int Width
        {
            get { return _width; }
        }

        /// <summary>
        /// Height of the document, in pixels. All contained layers must be this tall as well.
        /// </summary>
        public int Height
        {
            get { return _height; }
        }

        /// <summary>
        /// The size of the document, in pixels. This is a convenience property that wraps up
        /// the Width and Height properties in one Size structure.
        /// </summary>
        public Size Size
        {
            get { return new Size(Width, Height); }
        }

        public Rectangle Bounds
        {
            get { return new Rectangle(0, 0, Width, Height); }
        }

        /// <summary>
        /// Keeps track of whether the document has changed at all since it was last opened
        /// or saved. This is something that is not reset to true by any method in the Document
        /// class, but is set to false anytime anything is changed.
        /// This way we can prompt the user to save a changed document when they go to quit.
        /// </summary>
        public bool Dirty
        {
            get
            {
                if (this.disposed)
                {
                    throw new ObjectDisposedException("Document");
                }

                return this._dirty;
            }

            set
            {
                if (this.disposed)
                {
                    throw new ObjectDisposedException("Document");
                }

                if (this._dirty != value)
                {
                    this._dirty = value;
                    OnDirtyChanged();
                }
            }
        }

        [field: NonSerialized]
        public event EventHandler DirtyChanged;

        private void OnDirtyChanged()
        {
            if (DirtyChanged != null)
            {
                DirtyChanged(this, EventArgs.Empty);
            }
        }

        public DocumentMetadata Metadata
        {
            get
            {
                if (_metadata == null)
                {
                    _metadata = new DocumentMetadata(_userMetaData);
                }

                return _metadata;
            }
        }

        #endregion

        #region Metadata

        [NonSerialized]
        private DocumentMetadata _metadata;

        public void ReplaceMetaDataFrom(Document other)
        {
            this.Metadata.ReplaceWithDataFrom(other.Metadata);
        }

        public void ClearMetaData()
        {
            this.Metadata.Clear();
        }

        #endregion

        #region Render

        /// <summary>
        /// Clears a portion of a surface to transparent.
        /// </summary>
        /// <param name="surface">The surface to partially clear</param>
        /// <param name="roi">The rectangle to clear</param>
        private unsafe void ClearBackground(Surface surface, Rectangle roi)
        {
            roi.Intersect(surface.Bounds);

            for (int y = roi.Top; y < roi.Bottom; y++)
            {
                ColorBgra* ptr = surface.UnsafeGetPointAddress(roi.Left, y);
                Memory.SetToZero(ptr, (ulong)roi.Width * ColorBgra.Size);
            }
        }

        /// <summary>
        /// Clears a portion of a surface to transparent.
        /// </summary>
        /// <param name="surface">The surface to partially clear</param>
        /// <param name="rois">The array of Rectangles designating the areas to clear</param>
        /// <param name="startIndex">The start index within the rois array to clear</param>
        /// <param name="length">The number of Rectangles in the rois array (staring with startIndex) to clear</param>
        private void ClearBackground(Surface surface, Rectangle[] rois, int startIndex, int length)
        {
            for (int i = startIndex; i < startIndex + length; i++)
            {
                ClearBackground(surface, rois[i]);
            }
        }

        public void Render(RenderArgs args)
        {
            Render(args, args.Surface.Bounds);
        }

        public void Render(RenderArgs args, Rectangle roi)
        {
            Render(args, roi, false);
        }

        public void Render(RenderArgs args, bool clearBackground)
        {
            Render(args, args.Surface.Bounds, clearBackground);
        }

        /// <summary>
        /// Renders a requested region of the document. Will clear the background of the input
        /// before rendering if requested.
        /// </summary>
        /// <param name="args">Contains information used to control where rendering occurs.</param>
        /// <param name="roi">The rectangular region to render.</param>
        /// <param name="clearBackground">If true, 'args' will be cleared to zero before rendering.</param>
        public void Render(RenderArgs args, Rectangle roi, bool clearBackground)
        {
            int startIndex;

            if (clearBackground)
            {
                BitmapLayer layer0;
                layer0 = this._layers[0] as BitmapLayer;

                // Special case: if the first layer is a visible BitmapLayer with full opacity using 
                // the default blend op, we can just copy the pixels straight over
                if (layer0 != null &&
                    layer0.Visible &&
                    layer0.Opacity == 255 &&
                    layer0.BlendOp.GetType() == UserBlendOps.GetDefaultBlendOp())
                {
                    args.Surface.CopySurface(layer0.Surface);
                    startIndex = 1;
                }
                else
                {
                    ClearBackground(args.Surface, roi);
                    startIndex = 0;
                }
            }
            else
            {
                startIndex = 0;
            }

            for (int i = startIndex; i < this._layers.Count; ++i)
            {
                ImageLayer layer = (ImageLayer)this._layers[i];

                if (layer.Visible)
                {
                    layer.Render(args, roi);
                }
            }
        }

        public void Render(RenderArgs args, Rectangle[] roi, bool clearBackground)
        {
            this.Render(args, roi, 0, roi.Length, clearBackground);
        }

        public void Render(RenderArgs args, Rectangle[] roi, int startIndex, int length, bool clearBackground)
        {
            int startLayerIndex;

            if (clearBackground)
            {
                BitmapLayer layer0;
                layer0 = this._layers[0] as BitmapLayer;

                // Special case: if the first layer is a visible BitmapLayer with full opacity using 
                // the default blend op, we can just copy the pixels straight over
                if (layer0 != null &&
                    layer0.Visible &&
                    layer0.Opacity == 255 &&
                    layer0.BlendOp.GetType() == UserBlendOps.GetDefaultBlendOp())
                {
                    args.Surface.CopySurface(layer0.Surface, roi, startIndex, length);
                    startLayerIndex = 1;
                }
                else
                {
                    ClearBackground(args.Surface, roi, startIndex, length);
                    startLayerIndex = 0;
                }
            }
            else
            {
                startLayerIndex = 0;
            }

            for (int i = startLayerIndex; i < this._layers.Count; ++i)
            {
                ImageLayer layer = (ImageLayer)this._layers[i];

                if (layer.Visible)
                {
                    layer.RenderUnchecked(args, roi, startIndex, length);
                }
            }
        }

        /// <summary>
        /// Renders only the portions of the document that have changed (been Invalidated) since 
        /// the last call to this function.
        /// </summary>
        /// <param name="args">Contains information used to control where rendering occurs.</param>
        /// <returns>true if any rendering was done (the update list was non-empty), false otherwise</returns>
        public bool Update(RenderArgs dst)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("Document");
            }

            Rectangle[] updateRects;
            int updateRectsLength;
            _updateRegion.GetArrayReadOnly(out updateRects, out updateRectsLength);

            if (updateRectsLength == 0)
            {
                return false;
            }

            GeometryRegion region = Utility.RectanglesToRegion(updateRects, 0, updateRectsLength);
            Rectangle[] rectsOriginal = region.GetRegionScansReadOnlyInt();
            Rectangle[] rectsToUse;

            // Special case where we're drawing 1 big rectangle: split it up!
            // This case happens quite frequently, but we don't want to spend a lot of
            // time analyzing any other case that is more complicated.
            if (rectsOriginal.Length == 1 && rectsOriginal[0].Height > 1)
            {
                Rectangle[] rectsNew = new Rectangle[Processor.LogicalCpuCount];
                Utility.SplitRectangle(rectsOriginal[0], rectsNew);
                rectsToUse = rectsNew;
            }
            else
            {
                rectsToUse = rectsOriginal;
            }

            int cpuCount = Processor.LogicalCpuCount;
            for (int i = 0; i < cpuCount; ++i)
            {
                int start = (i * rectsToUse.Length) / cpuCount;
                int end = ((i + 1) * rectsToUse.Length) / cpuCount;

                var usc = new UpdateScansContext(this, dst, rectsToUse, start, end - start);

                if (i == cpuCount - 1)
                {
                    // Reuse this thread for the last job -- no sense creating a new thread.
                    usc.UpdateScans(usc);
                }
                else
                {
                    _threadPool.QueueTask(new WaitCallback(usc.UpdateScans), usc);
                }
            }

            this._threadPool.Drain();
            Validate();
            return true;
        }

        #endregion

        #region Instance

        /// <summary>
        /// Constructs a blank document (zero layers) of the given width and height.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public Document(int width, int height)
        {
            this._width = width;
            this._height = height;
            this.Dirty = true;
            this._updateRegion = new Vector<Rectangle>();
            _layers = new ImageLayerList(this);
            SetupEvents();
            _userMetaData = new NameValueCollection();
            Invalidate();
        }

        public Document(Size size)
            : this(size.Width, size.Height)
        {
        }

        #endregion


        #region Invalidate

        [field: NonSerialized]
        public event InvalidateEventHandler Invalidated;

        /// <summary>
        /// Raises the Invalidated event.
        /// </summary>
        /// <param name="e"></param>
        private void OnInvalidated(InvalidateEventArgs e)
        {
            if (Invalidated != null)
            {
                Invalidated(this, e);
            }
        }

        /// <summary>
        /// Handles the Changing event that is raised from the contained LayerList.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LayerListChangingHandler(object sender, EventArgs e)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("Document");
            }

            foreach (ImageLayer layer in Layers)
            {
                layer.Invalidated -= _layerInvalidatedDelegate;
            }
        }

        /// <summary>
        /// Handles the Changed event that is raised from the contained LayerList.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LayerListChangedHandler(object sender, EventArgs e)
        {
            foreach (ImageLayer layer in Layers)
            {
                layer.Invalidated += _layerInvalidatedDelegate;
            }

            Invalidate();
        }

        /// <summary>
        /// Handles the Invalidated event that is raised from any contained Layer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LayerInvalidatedHandler(object sender, InvalidateEventArgs e)
        {
            Invalidate(e.InvalidRect);
        }

        /// <summary>
        /// Causes the whole document to be invalidated, forcing a full rerender on
        /// the next call to Update.
        /// </summary>
        public void Invalidate()
        {
            Dirty = true;
            Rectangle rect = new Rectangle(0, 0, Width, Height);
            _updateRegion.Clear();
            _updateRegion.Add(rect);
            OnInvalidated(new InvalidateEventArgs(rect));
        }

        /// <summary>
        /// Invalidates a portion of the document. The given region is then tagged
        /// for rerendering during the next call to Update.
        /// </summary>
        /// <param name="roi">The region of interest to be invalidated.</param>
        public void Invalidate(GeometryRegion roi)
        {
            Dirty = true;

            foreach (Rectangle rect in roi.GetRegionScansReadOnlyInt())
            {
                rect.Intersect(this.Bounds);
                _updateRegion.Add(rect);

                if (!rect.IsEmpty)
                {
                    InvalidateEventArgs iea = new InvalidateEventArgs(rect);
                    OnInvalidated(iea);
                }
            }
        }

        public void Invalidate(RectangleF[] roi)
        {
            foreach (RectangleF rectF in roi)
            {
                Invalidate(Rectangle.Truncate(rectF));
            }
        }

        public void Invalidate(RectangleF roi)
        {
            Invalidate(Rectangle.Truncate(roi));
        }

        public void Invalidate(Rectangle[] roi)
        {
            foreach (Rectangle rect in roi)
            {
                Invalidate(rect);
            }
        }

        /// <summary>
        /// Invalidates a portion of the document. The given region is then tagged
        /// for rerendering during the next call to Update.
        /// </summary>
        /// <param name="roi">The region of interest to be invalidated.</param>
        public void Invalidate(Rectangle roi)
        {
            Dirty = true;
            Rectangle rect = Rectangle.Intersect(roi, this.Bounds);
            _updateRegion.Add(rect);
            OnInvalidated(new InvalidateEventArgs(rect));
        }

        /// <summary>
        /// Clears the document's update region. This is called at the end of the
        /// Update method.
        /// </summary>
        private void Validate()
        {
            _updateRegion.Clear();
        }

        #endregion

        #region Load document

        /// <summary>
        /// Creates a document that consists of one BitmapLayer.
        /// </summary>
        /// <param name="image">The Image to make a copy of that will be the first layer ("Background") in the document.</param>
        public static Document FromImage(Image image)
        {
            if (image == null)
            {
                throw new ArgumentNullException("image");
            }

            var document = new Document(image.Width, image.Height);
            BitmapLayer layer = ImageLayer.CreateBackgroundLayer(image.Width, image.Height);
            layer.Surface.Clear(ColorBgra.FromBgra(0, 0, 0, 0));

            Bitmap asBitmap = image as Bitmap;

            // Copy pixels
            if (asBitmap != null && asBitmap.PixelFormat == PixelFormat.Format32bppArgb)
            {
                unsafe
                {
                    BitmapData bData = asBitmap.LockBits(new Rectangle(0, 0, asBitmap.Width, asBitmap.Height),
                        ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                    try
                    {
                        for (int y = 0; y < bData.Height; ++y)
                        {
                            uint* srcPtr = (uint*)((byte*)bData.Scan0.ToPointer() + (y * bData.Stride));
                            ColorBgra* dstPtr = layer.Surface.GetRowAddress(y);

                            for (int x = 0; x < bData.Width; ++x)
                            {
                                dstPtr->Bgra = *srcPtr;
                                ++srcPtr;
                                ++dstPtr;
                            }
                        }
                    }

                    finally
                    {
                        asBitmap.UnlockBits(bData);
                    }
                }
            }
            else if (asBitmap != null && asBitmap.PixelFormat == PixelFormat.Format24bppRgb)
            {
                unsafe
                {
                    BitmapData bData = asBitmap.LockBits(new Rectangle(0, 0, asBitmap.Width, asBitmap.Height),
                        ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                    try
                    {
                        for (int y = 0; y < bData.Height; ++y)
                        {
                            byte* srcPtr = (byte*)bData.Scan0.ToPointer() + (y * bData.Stride);
                            ColorBgra* dstPtr = layer.Surface.UnsafeGetRowAddress(y);

                            for (int x = 0; x < bData.Width; ++x)
                            {
                                byte b = *srcPtr;
                                byte g = *(srcPtr + 1);
                                byte r = *(srcPtr + 2);
                                byte a = 255;

                                *dstPtr = ColorBgra.FromBgra(b, g, r, a);

                                srcPtr += 3;
                                ++dstPtr;
                            }
                        }
                    }

                    finally
                    {
                        asBitmap.UnlockBits(bData);
                    }
                }
            }
            else
            {
                using (var args = new RenderArgs(layer.Surface))
                {
                    args.Graphics.CompositingMode = CompositingMode.SourceCopy;
                    args.Graphics.SmoothingMode = SmoothingMode.None;
                    args.Graphics.DrawImage(image, args.Bounds, args.Bounds, GraphicsUnit.Pixel);
                }
            }

            // Transfer metadata

            // Sometimes GDI+ does not honor the resolution tags that we
            // put in manually via the EXIF properties.
            document.DpuUnit = MeasurementUnit.Inch;
            document.DpuX = image.HorizontalResolution;
            document.DpuY = image.VerticalResolution;

            PropertyItem[] pis;

            try
            {
                pis = image.PropertyItems;
            }

            catch (Exception ex)
            {
                pis = null;
                // ignore the error and continue on
            }

            if (pis != null)
            {
                for (int i = 0; i < pis.Length; ++i)
                {
                    document.Metadata.AddExifValues(new PropertyItem[] { pis[i] });
                }
            }

            // Finish up
            document.Layers.Add(layer);
            document.Invalidate();
            return document;
        }

        public static byte[] MagicBytes
        {
            get { return Encoding.UTF8.GetBytes("PDN3"); }
        }

        /// <summary>
        /// Deserializes a Document from a stream.
        /// </summary>
        /// <param name="stream">The stream to deserialize from. This stream must be seekable.</param>
        /// <returns>The Document that was stored in stream.</returns>
        /// <remarks>
        /// This is the only supported way to deserialize a Document instance from disk.
        /// </remarks>
        public static Document FromStream(Stream stream)
        {
            long oldPosition = stream.Position;
            bool pdn21Format = true;

            // Version 2.1+ file format:
            //   Starts with bytes as defined by MagicBytes 
            //   Next three bytes are 24-bit unsigned int 'N' (first byte is low-word, second byte is middle-word, third byte is high word)
            //   The next N bytes are a string, this is the document header (it is XML, UTF-8 encoded)
            //       Important: 'N' indicates a byte count, not a character count. 'N' bytes may result in less than 'N' characters,
            //                  depending on how the characters decode as per UTF8
            //   If the next 2 bytes are 0x00, 0x01: This signifies that non-compressed .NET serialized data follows.
            //   If the next 2 bytes are 0x1f, 0x8b: This signifies the start of the gzip compressed .NET serialized data
            //
            // Version 2.0 and previous file format:
            //   Starts with 0x1f, 0x8b: this signifies the start of the gzip compressed .NET serialized data.

            // Read in the 'magic' bytes
            for (int i = 0; i < MagicBytes.Length; ++i)
            {
                int theByte = stream.ReadByte();

                if (theByte == -1)
                {
                    throw new EndOfStreamException();
                }

                if (theByte != MagicBytes[i])
                {
                    pdn21Format = false;
                    break;
                }
            }

            // Read in the header if we found the 'magic' bytes identifying a PDN 2.1 file
            XmlDocument headerXml = null;
            if (pdn21Format)
            {
                // This is a Paint.NET v2.1+ file.  
                int low = stream.ReadByte();

                if (low == -1)
                {
                    throw new EndOfStreamException();
                }

                int mid = stream.ReadByte();

                if (mid == -1)
                {
                    throw new EndOfStreamException();
                }

                int high = stream.ReadByte();

                if (high == -1)
                {
                    throw new EndOfStreamException();
                }

                int byteCount = low + (mid << 8) + (high << 16);
                byte[] bytes = new byte[byteCount];
                int bytesRead = Utility.ReadFromStream(stream, bytes, 0, byteCount);

                if (bytesRead != byteCount)
                {
                    throw new EndOfStreamException("expected " + byteCount + " bytes, but only got " + bytesRead);
                }

                string xml = Encoding.UTF8.GetString(bytes);
                headerXml = new XmlDocument();
                headerXml.LoadXml(xml);
            }
            else
            {
                stream.Position = oldPosition; // rewind and try as v2.0-or-earlier file
            }

            // Start reading the data section of the file. Determine if it's gzip or regular
            long oldPosition2 = stream.Position;
            int first = stream.ReadByte();

            if (first == -1)
            {
                throw new EndOfStreamException();
            }

            int second = stream.ReadByte();

            if (second == -1)
            {
                throw new EndOfStreamException();
            }

            object docObject;
            var formatter = new BinaryFormatter();
            var sfb = new SerializationFallbackBinder();

            sfb.AddAssembly(Assembly.GetExecutingAssembly()); // first try PaintDotNet.Data.dll
            sfb.AddAssembly(typeof(Utility).Assembly); // second, try PaintDotNet.Core.dll
            sfb.AddAssembly(typeof(Memory).Assembly); // third, try PaintDotNet.SystemLayer.dll
            formatter.Binder = sfb;

            if (first == 0 && second == 1)
            {
                var deferred = new DeferredFormatter();
                formatter.Context = new StreamingContext(formatter.Context.State, deferred);
                docObject = formatter.UnsafeDeserialize(stream, null);
                deferred.FinishDeserialization(stream);
            }
            else if (first == 0x1f && second == 0x8b)
            {
                stream.Position = oldPosition2; // rewind to the start of 0x1f, 0x8b
                GZipStream gZipStream = new GZipStream(stream, CompressionMode.Decompress, true);
                docObject = formatter.UnsafeDeserialize(gZipStream, null);
            }
            else
            {
                throw new FormatException("file is not a valid Paint.NET document");
            }

            var document = (Document)docObject;
            document.Dirty = true;
            document._headerXml = headerXml;
            document.Invalidate();
            return document;
        }

        /// <summary>
        /// Called after deserialization occurs so that certain things that are non-serializable
        /// can be set up.
        /// </summary>
        public void OnDeserialization(object sender)
        {
            this._updateRegion = new Vector<Rectangle>();
            this._updateRegion.Add(this.Bounds);
            this._threadPool = new ThreadPool();
            SetupEvents();
            Dirty = true;
        }

        #endregion

        #region Xml

        [NonSerialized]
        private XmlDocument _headerXml;

        private const string HeaderXmlSkeleton = "<ptnImage><custom></custom></ptnImage>";

        private XmlDocument HeaderXml
        {
            get
            {
                if (this.disposed)
                {
                    throw new ObjectDisposedException("Document");
                }

                if (this._headerXml == null)
                {
                    this._headerXml = new XmlDocument();
                    this._headerXml.LoadXml(HeaderXmlSkeleton);
                }

                return this._headerXml;
            }
        }

        public string Header
        {
            get
            {
                if (this.disposed)
                {
                    throw new ObjectDisposedException("Document");
                }

                return this.HeaderXml.OuterXml;
            }
        }

        public string CustomHeaders
        {
            get
            {
                if (this.disposed)
                {
                    throw new ObjectDisposedException("Document");
                }

                return this.HeaderXml.SelectSingleNode("/ptnImage/custom").InnerXml;
            }

            set
            {
                if (this.disposed)
                {
                    throw new ObjectDisposedException("Document");
                }

                this.HeaderXml.SelectSingleNode("/ptnImage/custom").InnerXml = value;
                Dirty = true;
            }
        }

        private void PrepareHeader()
        {
            XmlDocument xd = this.HeaderXml;
            XmlElement pdnImage = (XmlElement)xd.SelectSingleNode("/pdnImage");
            pdnImage.SetAttribute("width", this.Width.ToString());
            pdnImage.SetAttribute("height", this.Height.ToString());
            pdnImage.SetAttribute("layers", this.Layers.Count.ToString());
            pdnImage.SetAttribute("savedWithVersion", this.SavedWithVersion.ToString(4));
        }

        #endregion

        #region Save

        /// <summary>
        /// Saves the Document to the given Stream with only the default headers and no
        /// IO completion callback.
        /// </summary>
        /// <param name="stream">The Stream to serialize the Document to.</param>
        public void SaveToStream(Stream stream)
        {
            SaveToStream(stream, null);
        }

        /// <summary>
        /// Saves the Document to the given Stream with the default and given headers, and
        /// using the given IO completion callback.
        /// </summary>
        /// <param name="stream">The Stream to serialize the Document to.</param>
        /// <param name="callback">
        /// This can be used to keep track of the number of uncompressed bytes that are written. The 
        /// values reported through the IOEventArgs.Count+Offset will vary from 1 to approximately 
        /// Layers.Count*Width*Height*sizeof(ColorBgra). The final number will actually be higher 
        /// because of hierarchical overhead, so make sure to cap any progress reports to 100%. This
        /// callback will be wired to the IOFinished event of a SiphonStream. Events may be raised
        /// from any thread. May be null.
        /// </param>
        public void SaveToStream(Stream stream, IOEventHandler callback)
        {
            InitializeDpu();

            PrepareHeader();
            string headerText = this.HeaderXml.OuterXml;

            // Write the header
            byte[] magicBytes = MagicBytes;
            stream.Write(magicBytes, 0, magicBytes.Length);
            byte[] headerBytes = Encoding.UTF8.GetBytes(headerText);
            stream.WriteByte((byte)(headerBytes.Length & 0xff));
            stream.WriteByte((byte)((headerBytes.Length & 0xff00) >> 8));
            stream.WriteByte((byte)((headerBytes.Length & 0xff0000) >> 16));
            stream.Write(headerBytes, 0, headerBytes.Length);
            stream.Flush();

            // Copy version info
            this._savedWith = PtnInfo.GetVersion();

            // Write 0x00, 0x01 to indicate normal .NET serialized data
            stream.WriteByte(0x00);
            stream.WriteByte(0x01);

            // Write the remainder of the file (gzip compressed)
            var siphonStream = new SiphonStream(stream);
            var formatter = new BinaryFormatter();
            var deferred = new DeferredFormatter(true, null);
            formatter.Context = new StreamingContext(formatter.Context.State, deferred);
            formatter.Serialize(siphonStream, this);
            deferred.FinishSerialization(siphonStream);

            stream.Flush();
        }

        /// <summary>
        /// Reports the version of Paint.NET that this file was saved with.
        /// This is reset when SaveToStream is used. This can be used to
        /// determine file format compatibility if necessary.
        /// </summary>
        public Version SavedWithVersion
        {
            get
            {
                if (disposed)
                {
                    throw new ObjectDisposedException("Document");
                }

                if (_savedWith == null)
                {
                    _savedWith = PtnInfo.GetVersion();
                }

                return _savedWith;
            }
        }

        private class SaveProgressRelay
        {
            private DeferredFormatter formatter;
            private IOEventHandler ioCallback;
            private long lastReportedBytes;

            public SaveProgressRelay(DeferredFormatter formatter, IOEventHandler ioCallback)
            {
                this.formatter = formatter;
                this.ioCallback = ioCallback;
                this.formatter.ReportedBytesChanged += new EventHandler(Formatter_ReportedBytesChanged);
            }

            private void Formatter_ReportedBytesChanged(object sender, EventArgs e)
            {
                long reportedBytes = formatter.ReportedBytes;
                bool raiseEvent;
                long length = 0;

                lock (this)
                {
                    raiseEvent = (reportedBytes > lastReportedBytes);

                    if (raiseEvent)
                    {
                        length = reportedBytes - this.lastReportedBytes;
                        this.lastReportedBytes = reportedBytes;
                    }
                }

                if (raiseEvent && ioCallback != null)
                {
                    ioCallback(this, new IOEventArgs(IOOperationType.Write, reportedBytes - length, (int)length));
                }
            }
        }

        #endregion

        #region Flatten document

        public void Flatten(Surface dst)
        {
            if (dst.Size != this.Size)
            {
                throw new ArgumentOutOfRangeException("dst.Size must match this.Size");
            }

            dst.Clear(ColorBgra.White.NewAlpha(0));

            using (var renderArgs = new RenderArgs(dst))
            {
                Render(renderArgs, true);
            }
        }

        /// <summary>
        /// Returns a new Document that is a flattened version of this one
        /// "Flattened" means it is one layer that is simply a bitmap of
        /// the compositied image.
        /// </summary>
        public Document Flatten()
        {
            var newDocument = new Document(_width, _height);
            newDocument.ReplaceMetaDataFrom(this);
            BitmapLayer layer = ImageLayer.CreateBackgroundLayer(_width, _height);
            newDocument.Layers.Add(layer);
            Flatten(layer.Surface);
            return newDocument;
        }

        #endregion

        #region Dispose

        ~Document()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool disposed = false;

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    foreach (ImageLayer layer in _layers)
                    {
                        layer.Dispose();
                    }
                }

                disposed = true;
            }
        }

        #endregion

        #region Clone

        public Document Clone()
        {
            var stream = new MemoryStream();
            SaveToStream(stream);
            stream.Seek(0, SeekOrigin.Begin);
            return FromStream(stream);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        #endregion

        /// <summary>
        /// Sets up event handling for contained objects.
        /// </summary>
        private void SetupEvents()
        {
            _layers.Changed += LayerListChangedHandler;
            _layers.Changing += LayerListChangingHandler;
            _layerInvalidatedDelegate = LayerInvalidatedHandler;

            foreach (ImageLayer layer in _layers)
            {
                layer.Invalidated += _layerInvalidatedDelegate;
            }
        }
    }

    /// <summary>
    /// Use to update a ScanLine, usually in thread task context data.
    /// </summary>
    public sealed class UpdateScansContext
    {
        private readonly Document _document;
        private readonly RenderArgs _dst;
        private readonly Rectangle[] _scans;
        private readonly int _startIndex;
        private readonly int _length;

        public void UpdateScans(object context)
        {
            _document.Render(_dst, _scans, _startIndex, _length, true);
        }

        public UpdateScansContext(Document document, RenderArgs dst, Rectangle[] scans, int startIndex, int length)
        {
            this._document = document;
            this._dst = dst;
            this._scans = scans;
            this._startIndex = startIndex;
            this._length = length;
        }
    }
}