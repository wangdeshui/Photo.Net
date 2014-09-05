using System.Windows.Forms;

namespace Photo.Net.Tool
{
    public abstract class BaseControl : UserControl
    {
        public abstract bool IsMouseCaptured();
    }
}
