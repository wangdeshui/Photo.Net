using System;
using System.Windows.Forms;
using Photo.Net.Base;

namespace Photo.Net.Tool.Controls
{
    public class PanelEx :
        ScrollPanel
    {
        private bool _hideHScroll;

        public bool HideHScroll
        {
            get
            {
                return this._hideHScroll;
            }

            set
            {
                this._hideHScroll = value;
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            if (this._hideHScroll)
            {
                UserInterface.SuspendControlPainting(this);
            }

            base.OnSizeChanged(e);

            if (this._hideHScroll)
            {
                UserInterface.HideHorizontalScrollBar(this);
                UserInterface.ResumeControlPainting(this);
                Invalidate(true);
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            //base.OnMouseWheel(e);
        }
    }
}
