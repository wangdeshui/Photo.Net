using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;

namespace Photo.Net.Gdi
{
    /// <summary>
    /// Re-implements System.Drawing.PropertyItem so that the data is serializable.
    /// </summary>
    [Serializable]
    internal sealed class ImageMetadata
    {
        private const string PiElementName = "exif";
        private const string IdPropertyName = "id";
        private const string LenPropertyName = "len";
        private const string TypePropertyName = "type";
        private const string ValuePropertyName = "value";

        private readonly byte[] _value;

        public int Id { get; private set; }

        public int Len { get; private set; }

        public short Type { get; private set; }

        public byte[] Value
        {
            get
            {
                return (byte[])_value.Clone();
            }
        }

        public ImageMetadata(int id, int len, short type, byte[] value)
        {
            this.Id = id;
            this.Len = len;
            this.Type = type;

            if (value == null)
            {
                this._value = new byte[0];
            }
            else
            {
                this._value = (byte[])value.Clone();
            }

            if (len != this._value.Length)
            {
            }
        }

        public string ToBlob()
        {
            string blob = string.Format("<{0} {1}=\"{2}\" {3}=\"{4}\" {5}=\"{6}\" {7}=\"{8}\" />",
                PiElementName,
                IdPropertyName, this.Id.ToString(CultureInfo.InvariantCulture),
                LenPropertyName, this.Len.ToString(CultureInfo.InvariantCulture),
                TypePropertyName, this.Type.ToString(CultureInfo.InvariantCulture),
                ValuePropertyName, Convert.ToBase64String(this._value));

            return blob;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public PropertyItem ToPropertyItem()
        {
            PropertyItem pi = GetPropertyItem();

            pi.Id = this.Id;
            pi.Len = this.Len;
            pi.Type = this.Type;
            pi.Value = this.Value;

            return pi;
        }

        public static ImageMetadata FromPropertyItem(PropertyItem item)
        {
            return new ImageMetadata(item.Id, item.Len, item.Type, item.Value);
        }

        private static string GetProperty(string blob, string propertyName)
        {
            string findMe = propertyName + "=\"";
            int startIndex = blob.IndexOf(findMe, StringComparison.Ordinal) + findMe.Length;
            int endIndex = blob.IndexOf("\"", startIndex, StringComparison.Ordinal);
            string propertyValue = blob.Substring(startIndex, endIndex - startIndex);
            return propertyValue;
        }

        public static ImageMetadata FromBlob(string blob)
        {
            string idStr = GetProperty(blob, IdPropertyName);
            string lenStr = GetProperty(blob, LenPropertyName);
            string typeStr = GetProperty(blob, TypePropertyName);
            string valueStr = GetProperty(blob, ValuePropertyName);

            int id = int.Parse(idStr, CultureInfo.InvariantCulture);
            int len = int.Parse(lenStr, CultureInfo.InvariantCulture);
            short type = short.Parse(typeStr, CultureInfo.InvariantCulture);
            byte[] value = Convert.FromBase64String(valueStr);

            var meataData = new ImageMetadata(id, len, type, value);

            return meataData;
        }

        // System.Drawing.Imaging.PropertyItem does not have a public constructor
        // So, as per the documentation, we have to "steal" one.
        // Quite ridiculous.
        // This depends on PropertyItem.png being an embedded resource in this assembly.
        private static Image _propertyItemImage;

        [MethodImpl(MethodImplOptions.Synchronized)]
        private static PropertyItem GetPropertyItem()
        {
            if (_propertyItemImage == null)
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Photo.Net.Gdi.PropertyItem.png");

                _propertyItemImage = Image.FromStream(stream);
            }

            PropertyItem pi = _propertyItemImage.PropertyItems[0];
            pi.Id = 0;
            pi.Len = 0;
            pi.Type = 0;
            pi.Value = new byte[0];

            return pi;
        }
    }
}