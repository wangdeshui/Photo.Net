using System.Windows.Forms;
using Photo.Net.Documents;
using Photo.Net.IO;

namespace Photo.Net
{
    public partial class MainWindow : Form
    {
        private readonly DocumentWorkspace _workspace;

        public MainWindow()
        {
            CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();


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

            Controls.Add(_workspace);

            _workspace.MouseWheel += _workspace_MouseWheel;
        }

        void _workspace_MouseWheel(object sender, MouseEventArgs e)
        {

            _workspace.PerformMouseWheel((Control)sender, e);
        }
    }
}
