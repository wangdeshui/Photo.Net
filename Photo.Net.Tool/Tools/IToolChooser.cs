using System;
using System.Windows.Forms;
using Photo.Net.Tool.Core;

namespace Photo.Net.Tool.Tools
{
    public interface IToolChooser
    {
        void SelectTool(Type toolType);
        void SelectTool(Type toolType, bool raiseEvent);
        void SetTools(ToolInfo[] toolInfos);
        event ToolClickedEventHandler ToolClicked;
    }

    public delegate void ToolClickedEventHandler(object sender, ToolClickedEventArgs e);

    public class ToolClickedEventArgs
        : System.EventArgs
    {
        private Type toolType;
        public Type ToolType
        {
            get
            {
                return toolType;
            }
        }

        public ToolClickedEventArgs(Control tool)
        {
            this.toolType = tool.GetType();
        }

        public ToolClickedEventArgs(Type toolType)
        {
            this.toolType = toolType;
        }
    }
}
