using System;
using System.IO;
using System.Linq;
using System.Text;
using Photo.Net.Base.Delegate;
using Photo.Net.Core;
using Photo.Net.Tool.Documents;
using Photo.Net.Tool.IO.Save;

namespace Photo.Net.Tool.IO
{
    /// <summary>
    /// Represents one type of image file.
    /// </summary>
    public abstract class FileType
    {
        #region Property

        private readonly string[] _extensions;
        private readonly string _name;
        private readonly FileTypeFlags _flags;

        public string[] Extensions
        {
            get { return (string[])this._extensions.Clone(); }
        }

        public string DefaultExtension
        {
            get { return this._extensions[0]; }
        }

        /// <summary>
        /// Returns the friendly name of the file type, such as "Bitmap" or "JPEG".
        /// </summary>
        public string Name
        {
            get { return this._name; }
        }

        #endregion

        protected FileType(string name, FileTypeFlags flags, string[] extensions)
        {
            this._name = name;
            this._flags = flags;
            this._extensions = (string[])extensions.Clone();
        }

        #region Flags

        public FileTypeFlags Flags
        {
            get { return this._flags; }
        }

        public bool SupportsLayers
        {
            get { return (this._flags & FileTypeFlags.SupportsLayers) != 0; }
        }

        public bool SupportsCustomHeaders
        {
            get { return (this._flags & FileTypeFlags.SupportsCustomHeaders) != 0; }
        }

        public bool SupportsSaving
        {
            get { return (this._flags & FileTypeFlags.SupportsSaving) != 0; }
        }

        public bool SupportsLoading
        {
            get { return (this._flags & FileTypeFlags.SupportsLoading) != 0; }
        }

        public bool SavesWithProgress
        {
            get { return (this._flags & FileTypeFlags.SavesWithProgress) != 0; }
        }

        public bool SupportsExtension(string ext)
        {
            return _extensions.Any(ext2 => 0 == string.Compare(ext2, ext, StringComparison.InvariantCultureIgnoreCase));
        }

        #endregion

        #region Save

        public void Save(Document input, Stream output, SaveConfig token, Surface scratchSurface,
            ProgressEventHandler callback, bool rememberToken)
        {
            if (!this.SupportsSaving)
            {
                throw new NotImplementedException("Saving is not supported by this FileType");
            }
            else
            {
                Surface disposeMe = null;

                if (scratchSurface == null)
                {
                    disposeMe = new Surface(input.Size);
                    scratchSurface = disposeMe;
                }
                else if (scratchSurface.Size != input.Size)
                {
                    throw new ArgumentException("scratchSurface.Size must equal input.Size");
                }

                OnSave(input, output, token, scratchSurface, callback);

                if (disposeMe != null)
                {
                    disposeMe.Dispose();
                }
            }
        }

        protected virtual void OnSave(Document input, Stream output, SaveConfig token, Surface scratchSurface,
            ProgressEventHandler callback)
        {
        }

        #endregion

        #region Save config

        public virtual SaveConfigWidget CreateSaveConfigWidget()
        {
            return new SaveConfigWidget(this);
        }

        public bool SupportsConfiguration
        {
            get
            {
                SaveConfig token = CreateDefaultSaveConfigToken();
                return !(token is NoSaveConfig);
            }
        }

        public SaveConfig CreateDefaultSaveConfigToken()
        {
            return OnCreateDefaultSaveConfigToken();
        }

        /// <summary>
        /// Creates a SaveConfig for this FileType with the default values.
        /// </summary>
        protected virtual SaveConfig OnCreateDefaultSaveConfigToken()
        {
            return new SaveConfig();
        }

        #endregion

        #region Load

        public Document Load(Stream input)
        {
            if (!this.SupportsLoading)
            {
                throw new NotSupportedException("Loading not supported for this FileType");
            }

            return OnLoad(input);
        }

        /// <summary>
        /// Load document from stream.
        /// </summary>
        protected abstract Document OnLoad(Stream input);

        #endregion

        #region Object

        public override bool Equals(object obj)
        {
            if (!(obj is FileType))
            {
                return false;
            }

            return this._name.Equals(((FileType)obj).Name);
        }

        public override int GetHashCode()
        {
            return this._name.GetHashCode();
        }

        public override string ToString()
        {
            var sb = new StringBuilder(_name);
            sb.Append(" (");

            for (int i = 0; i < _extensions.Length; ++i)
            {
                sb.Append("*");
                sb.Append(_extensions[i]);

                if (i != _extensions.Length - 1)
                {
                    sb.Append("; ");
                }
                else
                {
                    sb.Append(")");
                }
            }

            sb.Append("|");

            for (int i = 0; i < _extensions.Length; ++i)
            {
                sb.Append("*");
                sb.Append(_extensions[i]);

                if (i != _extensions.Length - 1)
                {
                    sb.Append(";");
                }
            }

            return sb.ToString();
        }

        #endregion

    }
}