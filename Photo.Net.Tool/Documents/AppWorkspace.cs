using System.Collections.Generic;
using System.Windows.Forms;
using Photo.Net.Tool.Tools;
using Photo.Net.Tool.Window;
using WeifenLuo.WinFormsUI.Docking;

namespace Photo.Net.Tool.Documents
{
    public class AppWorkspace
        : UserControl
    {
        private DocumentWorkspace _initialWorkspace;

        private readonly List<DocumentWorkspace> _documentWorkspaces = new List<DocumentWorkspace>();

        private readonly DockPanel _dockPanel = new DockPanel();
        private readonly ToolWindow _workspacesPanel = new ToolWindow();

        public DocumentWorkspace ActiveDocumentWorkspace { get; private set; }

        public AppWorkspace(DocumentWorkspace initialWorkspace)
        {
            _initialWorkspace = initialWorkspace;
            initialWorkspace.AppWorkspace = this;

            InitializeComponent();
            LayoutByCode(initialWorkspace);
            InitializeSetting();
        }


        private void InitializeComponent()
        {
            _dockPanel.Dock = _workspacesPanel.Dock = DockStyle.Fill;
            _dockPanel.DocumentStyle = DocumentStyle.DockingWindow;

            this.Controls.Add(_dockPanel);
        }

        public void LayoutByCode(DocumentWorkspace workspace)
        {
            var toolWindow = new ToolSet(workspace);
            toolWindow.Show(_dockPanel, DockState.DockLeft);

            _workspacesPanel.Controls.Add(_initialWorkspace);
            _workspacesPanel.Show(_dockPanel);
        }

        private void InitializeSetting()
        {
            ToolEnvironment = ToolEnvironment.GetDefaultAppEnvironment();
        }

        public ToolEnvironment ToolEnvironment { get; private set; }
    }
}
