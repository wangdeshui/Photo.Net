using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Photo.Net.Base.Delegate;
using Photo.Net.Core;
using Photo.Net.Core.Color;
using Photo.Net.Core.Struct;
using Photo.Net.Documents;
using Photo.Net.Gdi.Event;
using Photo.Net.Gdi.Graphic;
using Photo.Net.IO.Save;
using Photo.Net.Resource;

namespace Photo.Net.IO.Load
{
    /// <summary>
    /// Implements FileType for generic GDI+ codecs.
    /// </summary>
    /// <remarks>
    /// GDI+ file types do not support custom headers.
    /// </remarks>
    public class BitmapFileType
        : FileType
    {
        public ImageFormat ImageFormat { get; private set; }

        public static ImageCodecInfo GetImageCodecInfo(ImageFormat format)
        {
            ImageCodecInfo[] encoders = ImageCodecInfo.GetImageEncoders();

            return encoders.FirstOrDefault(icf => icf.FormatID == format.Guid);
        }

        #region Load

        protected override Document OnLoad(Stream input)
        {
            using (Image image = PdnResources.LoadImage(input))
            {
                Document document = Document.FromImage(image);
                return document;
            }
        }

        #endregion

        #region Init

        public BitmapFileType(string name, ImageFormat imageFormat, bool supportsLayers, string[] extensions)
            : this(name, imageFormat, supportsLayers, extensions, false)
        {
        }

        public BitmapFileType(string name, ImageFormat imageFormat, bool supportsLayers, string[] extensions,
            bool savesWithProgress)
            : base(name,
                (supportsLayers ? FileTypeFlags.SupportsLayers : FileTypeFlags.None) | FileTypeFlags.SupportsLoading |
                FileTypeFlags.SupportsSaving |
                (savesWithProgress ? FileTypeFlags.SavesWithProgress : FileTypeFlags.None),
                extensions)
        {
            this.ImageFormat = imageFormat;
        }

        #endregion

        #region Save

        protected override void OnSave(Document input, Stream output, SaveConfig token, Surface scratchSurface,
            ProgressEventHandler callback)
        {
            Save(input, output, scratchSurface, this.ImageFormat, callback);
        }

        public static void Save(Document input, Stream output, Surface scratchSurface, ImageFormat format,
            ProgressEventHandler callback)
        {
            scratchSurface.Clear(ColorBgra.FromBgra(0, 0, 0, 0));

            using (var ra = new RenderArgs(scratchSurface))
            {
                input.Render(ra, true);
            }

            using (Bitmap bitmap = scratchSurface.CreateAliasedBitmap())
            {
                LoadProperties(bitmap, input);
                bitmap.Save(output, format);
            }
        }

        public static void LoadProperties(Image dstImage, Document srcDoc)
        {
            var asBitmap = dstImage as Bitmap;

            if (asBitmap != null)
            {
                float dpiX;
                float dpiY;

                switch (srcDoc.DpuUnit)
                {
                    case MeasurementUnit.Centimeter:
                        dpiX = (float)Document.DotsPerCmToDotsPerInch(srcDoc.DpuX);
                        dpiY = (float)Document.DotsPerCmToDotsPerInch(srcDoc.DpuY);
                        break;

                    case MeasurementUnit.Inch:
                        dpiX = (float)srcDoc.DpuX;
                        dpiY = (float)srcDoc.DpuY;
                        break;

                    default:
                        dpiX = 1.0f;
                        dpiY = 1.0f;
                        break;
                }

                asBitmap.SetResolution(dpiX, dpiY);
            }

            DocumentMetadata metaData = srcDoc.Metadata;

            foreach (string key in metaData.GetKeys(DocumentMetadata.ExifSectionName))
            {
                string blob = metaData.GetValue(DocumentMetadata.ExifSectionName, key);
                PropertyItem pi = PtnGraphics.DeserializePropertyItem(blob);

                dstImage.SetPropertyItem(pi);
            }
        }
        #endregion
    }
}
