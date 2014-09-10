using System.Windows.Forms;
using Photo.Net.Tool.Documents;
using Photo.Net.Tool.IO;
using Photo.Net.Tool.Tools;

namespace Photo.Net.Forms
{
    public class MainPanel : UserControl
    {
        private readonly AppWorkspace _appworkspace;
        private readonly DocumentWorkspace _workspace;
        private readonly PanTool _moveTool;

        public MainPanel()
        {
            var dialog = new OpenFileDialog { CheckFileExists = true, CheckPathExists = true, FilterIndex = 0 };

            dialog.ShowDialog();

            var fileName = dialog.FileName;
            FileType type;

            var document = DocumentWorkspace.LoadDocument(this, fileName, out type, null);
            _workspace = new DocumentWorkspace
            {
                Document = document,
                Dock = DockStyle.Fill
            };
            _appworkspace = new AppWorkspace(_workspace) { Dock = DockStyle.Fill };

            Controls.Add(_appworkspace);
        }
    }
}
