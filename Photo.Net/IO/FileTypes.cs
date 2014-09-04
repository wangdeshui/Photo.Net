using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Imaging;
using Photo.Net.IO.Load;

namespace Photo.Net.IO
{
    /// <summary>
    /// Provides static method and properties for obtaining all the FileType objects
    /// responsible for loading and saving Document instances. Loads FileType plugins
    /// too.
    /// </summary>
    public static class FileTypes
    {
        public static ICollection<FileType> GetFileTypes()
        {
            return new Collection<FileType>
            {
                new BitmapFileType("jpeg", ImageFormat.Jpeg, false, new[] { "jpg", "jpeg" }),
                new IndexedColorFileType("index",new[] { "index"})
            };
        }
    }
}
