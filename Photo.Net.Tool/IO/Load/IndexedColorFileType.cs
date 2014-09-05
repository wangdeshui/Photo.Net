using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Photo.Net.Base.Delegate;
using Photo.Net.Core;
using Photo.Net.Tool.Documents;
using Photo.Net.Tool.IO.Save;

namespace Photo.Net.Tool.IO.Load
{
    /// <summary>
    ///  Implements FileType for IndexedColor bitmap.
    /// </summary>
    /// <remarks>
    /// IndexColor bitmap need a table store and recode the color index.
    /// </remarks>
    public class IndexedColorFileType
        : FileType
    {
        public IndexedColorFileType(string name, string[] extensions)
            : base(name, FileTypeFlags.SavesWithProgress |
            FileTypeFlags.SupportsCustomHeaders | FileTypeFlags.SupportsLayers |
            FileTypeFlags.SupportsLoading | FileTypeFlags.SupportsSaving, extensions)
        {
        }

        protected override Document OnLoad(Stream input)
        {
            var bmp = (Bitmap)Image.FromStream(input);

            var width = bmp.Width;
            var height = bmp.Height;

            var saveBmp = bmp.Clone(new Rectangle(0, 0, width, height), PixelFormat.Format8bppIndexed);

            //            saveBmp.Save(@"C:\Users\shinetech\Pictures\copy.jpg");
            return Document.FromImage(saveBmp);
        }

        protected override void OnSave(Document input, Stream output, SaveConfig token, Surface scratchSurface, ProgressEventHandler callback)
        {
            base.OnSave(input, output, token, scratchSurface, callback);
        }
    }
}
