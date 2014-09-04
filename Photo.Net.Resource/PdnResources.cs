using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Resources;

namespace Photo.Net.Resource
{
    public class PdnResources
    {
        public static ResourceManager Strings { get; set; }

        public static string GetString(string p)
        {
            return "";
        }

        public static Image GetImage(string p)
        {
            throw new System.NotImplementedException();
        }

        public static Stream GetResourceStream(string iconsPaintdotnetIco)
        {
            throw new System.NotImplementedException();
        }

        public static Image LoadImage(Stream input)
        {

            Image image = Image.FromStream(input);

            if (image.RawFormat == ImageFormat.Wmf || image.RawFormat == ImageFormat.Emf)
            {
                image.Dispose();
                throw new IOException("File format isn't supported");
            }

            return image;
        }
    }
}
