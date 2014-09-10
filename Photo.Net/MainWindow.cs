using System.Drawing;
using System.Windows.Forms;
using Photo.Net.Forms;
using Photo.Net.Tool.Window;
using WeifenLuo.WinFormsUI.Docking;

namespace Photo.Net
{
    public partial class MainWindow : BaseWindow
    {

        private readonly MainPanel _panel = new MainPanel() { Dock = DockStyle.Fill };

        public MainWindow()
        {
            InitializeComponent();

            Controls.Add(_panel);
        }

    }
}
