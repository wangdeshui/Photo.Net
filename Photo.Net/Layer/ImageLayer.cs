﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows.Forms;
using Photo.Net.Base.Delegate;
using Photo.Net.Core;
using Photo.Net.Core.Color;
using Photo.Net.Core.Geometry;
using Photo.Net.Documents;
using Photo.Net.Gdi.Event;
using Photo.Net.Resource;
using Photo.Net.Thumbnail;
using Photo.Net.Window;

namespace Photo.Net.Layer
{
    /// <summary>
    /// A layer's properties are immutable. That is, you can modify the surface
    /// of a layer all you want, but to change its dimensions requires creating
    /// a new layer.
    /// </summary>
    [Serializable]
    public abstract class ImageLayer
        : ICloneable,
          IDisposable,
          IThumbnailProvider,
        IPropertyChangeNotify
    {
        private readonly int _width;
        private readonly int _height;

        /// <summary>
        /// The background layer is generally opaque although it doesn't *have* to be. For
        /// example, the Canvas Size action distinguishes between background and non-background
        /// layers such that it fills the background layer with opaque and the non-background 
        /// layers with transparency.
        /// The value of this property should not be used to disallow the user from performing
        /// an action.
        /// </summary>
        public bool IsBackground
        {
            get
            {
                return properties.isBackground;
            }

            set
            {
                bool oldValue = properties.isBackground;

                if (oldValue != value)
                {
                    OnPropertyChanging(LayerProperties.IsBackgroundName);
                    properties.isBackground = value;
                    OnPropertyChanged(LayerProperties.IsBackgroundName);
                }
            }
        }

        /// <summary>
        /// If this value is non-0, then the PropertyChanged event will be
        /// suppressed. This is in place so that the Layer Properties dialog
        /// can tweak the properties without them filling up the undo stack.
        /// </summary>
        [NonSerialized]
        private int _suppressPropertyChanges;

        /// <summary>
        /// Encapsulates the mutable properties of the Layer class.
        /// </summary>
        [Serializable]
        internal sealed class LayerProperties
            : ICloneable,
              ISerializable
        {
            public string name;
            public NameValueCollection UserMetaData;
            public bool visible;
            public bool isBackground;
            public byte opacity;

            private const string nameTag = "name";
            private const string userMetaDataTag = "userMetaData";
            private const string visibleTag = "visible";
            private const string isBackgroundTag = "isBackground";
            private const string opacityTag = "opacity";

            public static string IsBackgroundName
            {
                get
                {
                    return PdnResources.GetString("Layer.Properties.IsBackground.Name");
                }
            }

            public static string NameName
            {
                get
                {
                    return PdnResources.GetString("Layer.Properties.Name.Name");
                }
            }

            public static string VisibleName
            {
                get
                {
                    return PdnResources.GetString("Layer.Properties.Visible.Name");
                }
            }

            public static string OpacityName
            {
                get
                {
                    return PdnResources.GetString("Layer.Properties.Opacity.Name");
                }
            }

            public LayerProperties(string name, NameValueCollection userMetaData, bool visible, bool isBackground, byte opacity)
            {
                this.name = name;
                this.UserMetaData = new NameValueCollection(userMetaData);
                this.visible = visible;
                this.isBackground = isBackground;
                this.opacity = opacity;
            }

            public LayerProperties(LayerProperties copyMe)
            {
                this.name = copyMe.name;
                this.UserMetaData = new NameValueCollection(copyMe.UserMetaData);
                this.visible = copyMe.visible;
                this.isBackground = copyMe.isBackground;
                this.opacity = copyMe.opacity;
            }

            public object Clone()
            {
                return new LayerProperties(this);
            }

            public LayerProperties(SerializationInfo info, StreamingContext context)
            {
                this.name = info.GetString(nameTag);
                this.UserMetaData = (NameValueCollection)info.GetValue(userMetaDataTag, typeof(NameValueCollection));
                this.visible = info.GetBoolean(visibleTag);
                this.isBackground = info.GetBoolean(isBackgroundTag);

                // This property was added with v2.1. So as to allow loading old .PDN files,
                // this is an optional item.
                // (Historical note: this property was actually moved from the BitmapLayer
                //  properties to the base class because it was found to be a rather important
                //  property for rendering regardless of layer "type")
                try
                {
                    this.opacity = info.GetByte(opacityTag);
                }

                catch (SerializationException)
                {
                    this.opacity = 255;
                }
            }

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue(nameTag, name);
                info.AddValue(userMetaDataTag, UserMetaData);
                info.AddValue(visibleTag, visible);
                info.AddValue(isBackgroundTag, isBackground);
                info.AddValue(opacityTag, opacity);
            }
        }

        private LayerProperties properties;

        public byte Opacity
        {
            get
            {
                if (disposed)
                {
                    throw new ObjectDisposedException("Layer");
                }

                return properties.opacity;
            }

            set
            {
                if (disposed)
                {
                    throw new ObjectDisposedException("Layer");
                }

                if (properties.opacity != value)
                {
                    OnPropertyChanging();
                    properties.opacity = value;
                    OnPropertyChanged();
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// Allows you to save the mutable properties of the layer so you can restore them later
        /// (esp. important for undo!). Mutable properties include the layer's name, whether it's
        /// visible, and the metadata. This list might expand later.
        /// </summary>
        /// <returns>
        /// An object that can be used later in a call to LoadProperties.
        /// </returns>
        /// <remarks>
        /// It is important that derived classes call this in the correct fashion so as to 'chain'
        /// the properties list together. The following is the correct pattern:
        /// 
        ///     public override object SaveProperties()
        ///     {
        ///         object baseProperties = base.SaveProperties();
        ///         return new List(properties.Clone(), new List(baseProperties, null));
        ///     }
        /// </remarks>
        public virtual object SaveProperties()
        {
            return properties.Clone();
        }

        public void LoadProperties(object oldState)
        {
            LoadProperties(oldState, false);
        }

        public virtual void LoadProperties(object oldState, bool suppressEvents)
        {
            LayerProperties lp = (LayerProperties)oldState;
            List<string> changed = new List<String>();

            if (!suppressEvents)
            {
                if (lp.name != properties.name)
                {
                    changed.Add(LayerProperties.NameName);
                }

                if (lp.isBackground != properties.isBackground)
                {
                    changed.Add(LayerProperties.IsBackgroundName);
                }

                if (lp.visible != properties.visible)
                {
                    changed.Add(LayerProperties.VisibleName);
                }

                if (lp.opacity != properties.opacity)
                {
                    changed.Add(LayerProperties.OpacityName);
                }
            }

            foreach (string propertyName in changed)
            {
                OnPropertyChanging(propertyName);
            }

            properties = (LayerProperties)((LayerProperties)oldState).Clone();

            Invalidate();

            foreach (string propertyName in changed)
            {
                OnPropertyChanged(propertyName);
            }
        }

        public int Width
        {
            get
            {
                return _width;
            }
        }

        public int Height
        {
            get
            {
                return _height;
            }
        }

        public Size Size
        {
            get
            {
                return new Size(Width, Height);
            }
        }

        public Rectangle Bounds
        {
            get
            {
                return new Rectangle(new Point(0, 0), Size);
            }
        }

        public void PushSuppressPropertyChanged()
        {
            Interlocked.Increment(ref _suppressPropertyChanges);
        }

        public void PopSuppressPropertyChanged()
        {
            if (0 > Interlocked.Decrement(ref _suppressPropertyChanges))
            {
                throw new InvalidProgramException("suppressPreviewChanged is less than zero");
            }
        }

        [field: NonSerialized]
        public event EventHandler PreviewChanged;

        protected virtual void OnPreviewChanged()
        {
            if (PreviewChanged != null)
            {
                PreviewChanged(this, EventArgs.Empty);
            }
        }

        #region Property change

        [field: NonSerialized]
        public event PropertyChangeHandler PropertyChanging;

        [field: NonSerialized]
        public event PropertyChangeHandler PropertyChanged;

        protected virtual void OnPropertyChanging([CallerMemberName] string propertyName = "")
        {
            if (this._suppressPropertyChanges == 0)
            {
                if (PropertyChanging != null)
                {
                    PropertyChanging(this, new PropertyChangeArgs(propertyName));
                }
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (this._suppressPropertyChanges == 0)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangeArgs(propertyName));
                }
            }
        }

        #endregion

        /// <summary>
        /// A user-definable name.
        /// </summary>
        public string Name
        {
            get
            {
                return properties.name;
            }

            set
            {
                if (properties.name != value)
                {
                    OnPropertyChanging(LayerProperties.NameName);
                    properties.name = value;
                    OnPropertyChanged(LayerProperties.NameName);
                }
            }
        }

        [NonSerialized]
        private DocumentMetadata metadata;

        public DocumentMetadata Metadata
        {
            get
            {
                if (metadata == null)
                {
                    metadata = new DocumentMetadata(properties.UserMetaData);
                }

                return metadata;
            }
        }

        /// <summary>
        /// Determines whether the layer is part of a document's composition. If this
        /// property is false, the composition engine will ignore this layer.
        /// </summary>
        public bool Visible
        {
            get
            {
                return properties.visible;
            }

            set
            {
                bool oldValue = properties.visible;

                if (oldValue != value)
                {
                    OnPropertyChanging(LayerProperties.VisibleName);
                    properties.visible = value;
                    OnPropertyChanged(LayerProperties.VisibleName);
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// Determines whether a rectangle is fully in bounds or not. This is determined by checking
        /// to make sure the left, top, right, and bottom edges are within bounds.
        /// </summary>
        /// <param name="roi"></param>
        /// <returns></returns>
        private bool IsInBounds(Rectangle roi)
        {
            if (roi.Left < 0 || roi.Top < 0 || roi.Left >= Width || roi.Top >= Height ||
                roi.Right > Width || roi.Bottom > Height)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Implements IThumbnailProvider.RenderThumbnail().
        /// </summary>
        public abstract Surface RenderThumbnail(int maxEdgeLength);

        /// <summary>
        /// Causes the layer to render a given rectangle of interest (roi) to the given destination surface.
        /// </summary>
        /// <param name="args">Contains information about which objects to use for rendering</param>
        /// <param name="roi">The rectangular region to be rendered.</param>
        public void Render(RenderArgs args, Rectangle roi)
        {
            // the bitmap we're rendering to must match the size of the layer we're rendering from
            if (args.Surface.Width != Width || args.Surface.Height != Height)
            {
                throw new ArgumentException();
            }

            // the region of interest can not be out of bounds!
            if (!IsInBounds(roi))
            {
                throw new ArgumentOutOfRangeException("roi");
            }

            RenderImpl(args, roi);
        }

        /// <summary>
        /// Causes the layer to render a given region of interest (roi) to the given destination surface.
        /// </summary>
        /// <param name="args">Contains information about which objects to use for rendering</param>
        /// <param name="roi">The region to be rendered.</param>
        public void Render(RenderArgs args, GeometryRegion roi)
        {
            Rectangle roiBounds = roi.GetBoundsInt();

            if (!IsInBounds(roiBounds))
            {
                throw new ArgumentOutOfRangeException("roi");
            }

            Rectangle[] rects = roi.GetRegionScansReadOnlyInt();
            RenderImpl(args, rects);
        }

        public void RenderUnchecked(RenderArgs args, Rectangle[] roi, int startIndex, int length)
        {
            RenderImpl(args, roi, startIndex, length);
        }

        /// <summary>
        /// Override this method to provide your layer's rendering capabilities.
        /// </summary>
        /// <param name="args">Contains information about which objects to use for rendering</param>
        /// <param name="roi">The rectangular region to be rendered.</param>
        protected abstract void RenderImpl(RenderArgs args, Rectangle roi);

        protected void RenderImpl(RenderArgs args, Rectangle[] roi)
        {
            RenderImpl(args, roi, 0, roi.Length);
        }

        protected virtual void RenderImpl(RenderArgs args, Rectangle[] roi, int startIndex, int length)
        {
            for (int i = startIndex; i < startIndex + length; ++i)
            {
                RenderImpl(args, roi[i]);
            }
        }

        [field: NonSerialized]
        public event InvalidateEventHandler Invalidated;

        protected virtual void OnInvalidated(InvalidateEventArgs e)
        {
            if (Invalidated != null)
            {
                Invalidated(this, e);
            }
        }

        /// <summary>
        /// Causes the entire layer surface to be invalidated.
        /// </summary>
        public void Invalidate()
        {
            Rectangle rect = new Rectangle(0, 0, Width, Height);
            OnInvalidated(new InvalidateEventArgs(rect));
        }

        /// <summary>
        /// Causes a portion of the layer surface to be invalidated.
        /// </summary>
        /// <param name="roi">The region of interest to be invalidated.</param>
        public void Invalidate(GeometryRegion roi)
        {
            foreach (Rectangle rect in roi.GetRegionScansReadOnlyInt())
            {
                Invalidate(rect);
            }
        }

        /// <summary>
        /// Causes a portion of the layer surface to be invalidated.
        /// </summary>
        /// <param name="roi">The region of interest to be invalidated.</param>
        public void Invalidate(RectangleF[] roi)
        {
            foreach (RectangleF rectF in roi)
            {
                Invalidate(Rectangle.Truncate(rectF));
            }
        }

        /// <summary>
        /// Causes a portion of the layer surface to be invalidated.
        /// </summary>
        /// <param name="roi">The rectangle of interest to be invalidated.</param>
        public void Invalidate(Rectangle roi)
        {
            Rectangle rect = Rectangle.Intersect(roi, this.Bounds);
            // TODO: this is horrible for performance w.r.t. complex invalidation regions. Lots of heap pollution.
            //       fix that!
            OnInvalidated(new InvalidateEventArgs(rect));
        }

        public ImageLayer(int width, int height)
        {
            this._width = width;
            this._height = height;
            this.properties = new LayerProperties(null, new NameValueCollection(), true, false, 255);
        }

        protected ImageLayer(ImageLayer copyMe)
        {
            this._width = copyMe._width;
            this._height = copyMe._height;
            this.properties = (LayerProperties)copyMe.properties.Clone();
        }

        // TODO: add "name" parameter, keep this for legacy and fill it in with "Background"
        //       goal is to put complete burden of loc on the client
        public static BitmapLayer CreateBackgroundLayer(int width, int height)
        {
            // set colors to 0xffffffff
            // note: we use alpha of 255 here so that "invert colors" works as expected
            // that is, for just 1 layer we invert the initial white->black
            // but on subsequent layers we invert transparent white -> transparent black, which shows up as white for the most part
            BitmapLayer layer = new BitmapLayer(width, height, ColorBgra.White);

            layer.Name = PdnResources.GetString("Layer.Background.Name");

            // tag it as a background layer
            layer.properties.isBackground = true;

            return layer;
        }

        /// <summary>
        /// This allows a layer to provide a dialog for configuring
        /// the layer's properties.
        /// </summary>
        public abstract BaseWindow CreateConfigDialog();

        public abstract object Clone();

        ~ImageLayer()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;

                if (disposing)
                {
                }
            }
        }

    }
}