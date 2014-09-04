using System.Windows.Forms;

namespace Photo.Net
{
    public abstract class BaseControl : UserControl
    {
        public abstract bool IsMouseCaptured();
    }
}
