using System;
using System.Drawing;
using Photo.Net.Resource;
using Photo.Net.Tool.Core.Enums;

namespace Photo.Net.Tool.Core
{
    /// <summary>
    /// Description all about a tool instance
    /// </summary>
    public class ToolInfo
    {
        #region Propertys

        public string Name { get; private set; }

        public string HelpText { get; private set; }

        public ImageResource Image { get; private set; }

        public bool SkipIfActiveOnHotKey { get; private set; }

        public char HotKey { get; private set; }

        public Type ToolType { get; private set; }

        public ToolBarConfigItems ToolBarConfigItems { get; private set; }

        #endregion

        #region Equal

        public override bool Equals(object obj)
        {
            ToolInfo rhs = obj as ToolInfo;

            if (rhs == null)
            {
                return false;
            }

            return (this.Name == rhs.Name) &&
                   (this.HelpText == rhs.HelpText) &&
                   (this.HotKey == rhs.HotKey) &&
                   (this.SkipIfActiveOnHotKey == rhs.SkipIfActiveOnHotKey) &&
                   (this.ToolType == rhs.ToolType);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        #endregion

        public ToolInfo(
            string name,
            string helpText,
            ImageResource image,
            char hotKey,
            bool skipIfActiveOnHotKey,
            ToolBarConfigItems toolBarConfigItems,
            Type toolType)
        {
            this.Name = name;
            this.HelpText = helpText;
            this.Image = image;
            this.HotKey = hotKey;
            this.SkipIfActiveOnHotKey = skipIfActiveOnHotKey;
            this.ToolBarConfigItems = toolBarConfigItems;
            this.ToolType = toolType;
        }
    }
}
