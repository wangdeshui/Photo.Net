using System;
using System.Drawing;
using System.Windows.Forms;
using Photo.Net.Tool.Documents;
using Photo.Net.Tool.Window;

namespace Photo.Net.Tool.Tools
{
    public partial class ToolSet
        : ToolWindow
    {
        public ToolsControl Tools { get; private set; }
        public DocumentWorkspace DocumentWorkspace { get; private set; }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.ClientSize = new Size(Tools.Width, Tools.Height);
        }

        public ToolSet(DocumentWorkspace workspace)
        {
            InitializeComponent();

            DocumentWorkspace = workspace;

            Tools = new ToolsControl() { Dock = DockStyle.Fill };
            Tools.SetTools(DocumentWorkspace.ToolInfos);

            Controls.Add(Tools);

            Tools.ToolClicked += SelectTool;
        }

        private void SelectTool(object sender, ToolClickedEventArgs e)
        {
            DocumentWorkspace.SetTool(e.ToolType);
        }
    }
}
