using System.Drawing;
using System.IO;
using Photo.Net.Base.Delegate;
using Photo.Net.Core;
using Photo.Net.Core.Color;
using Photo.Net.Documents;
using Photo.Net.IO.Save;

namespace Photo.Net.IO.Load
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
            //            var doc = Document.FromStream(input);
            var bmp = (Bitmap)Image.FromStream(input);

            var table = IndexedColorTable.FromBitmap(bmp);

            return null;
        }

        protected override void OnSave(Document input, Stream output, SaveConfig token, Surface scratchSurface, ProgressEventHandler callback)
        {
            base.OnSave(input, output, token, scratchSurface, callback);
        }
    }
}
