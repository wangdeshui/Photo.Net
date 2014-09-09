using System.Windows.Forms;
using Photo.Net.Forms;
using Photo.Net.Tool.Window;
using WeifenLuo.WinFormsUI.Docking;

namespace Photo.Net
{
    public partial class MainWindow : BaseWindow
    {

        private DockPanel _dockPanel;
        private readonly MainPanel _pane2 = new MainPanel();

        public MainWindow()
        {
            AllowDrop = true;
            InitializeComponent();

            //            Controls.Add(_panel);
            //            Controls.Add(_pane2);
            //            _panel.Show(this);
            //            _pane2.Show(this);
            this.BringToFront();

            _dockPanel = new DockPanel() { Dock = DockStyle.Fill, DocumentStyle = DocumentStyle.DockingWindow };
            Controls.Add(_dockPanel);

            new ToolSet().Show(_dockPanel);
            _pane2.Show(_dockPanel);
        }

    }
}
