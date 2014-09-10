using System;
using System.Windows.Forms;

namespace Photo.Net.Tool
{
    public abstract class BaseControl : UserControl
    {
        public abstract bool IsMouseCaptured();

        #region MouseEvent

        public ControlBubbleEventHandler<EventArgs> BubbleMouseEnter;
        public void OnChildMouseEnter(Control child, EventArgs e)
        {
            if (BubbleMouseEnter != null)
            {
                BubbleMouseEnter(new ControlBubbleEventArgs<EventArgs>(child, e));
            }
        }

        public ControlBubbleEventHandler<EventArgs> BubbleMouseLeave;
        public void OnChildMouseLeave(Control child, EventArgs e)
        {
            if (BubbleMouseLeave != null)
            {
                BubbleMouseLeave(new ControlBubbleEventArgs<EventArgs>(child, e));
            }
        }

        public ControlBubbleEventHandler<MouseEventArgs> BubbleMouseMove;
        public void OnChildMouseMove(Control child, MouseEventArgs e)
        {
            if (BubbleMouseMove != null)
            {
                BubbleMouseMove(new ControlBubbleEventArgs<MouseEventArgs>(child, e));
            }
        }

        public ControlBubbleEventHandler<MouseEventArgs> BubbleMouseClick;
        public void OnChildMouseClick(Control child, MouseEventArgs e)
        {
            if (BubbleMouseClick != null)
            {
                BubbleMouseClick(new ControlBubbleEventArgs<MouseEventArgs>(child, e));
            }
        }

        public ControlBubbleEventHandler<MouseEventArgs> BubbleMouseDown;
        public void OnChildMouseDown(Control child, MouseEventArgs e)
        {
            if (BubbleMouseDown != null)
            {
                BubbleMouseDown(new ControlBubbleEventArgs<MouseEventArgs>(child, e));
            }
        }

        public ControlBubbleEventHandler<MouseEventArgs> BubbleMouseUp;
        public void OnChildMouseUp(Control child, MouseEventArgs e)
        {
            if (BubbleMouseUp != null)
            {
                BubbleMouseUp(new ControlBubbleEventArgs<MouseEventArgs>(child, e));
            }
        }

        public ControlBubbleEventHandler<MouseEventArgs> BubbleMouseWheel;
        public void OnChildMouseWheel(Control child, MouseEventArgs e)
        {
            if (BubbleMouseWheel != null)
            {
                BubbleMouseWheel(new ControlBubbleEventArgs<MouseEventArgs>(child, e));
            }
        }

        #endregion

        #region KeyEvent

        public ControlBubbleEventHandler<KeyEventArgs> BubbleKeyDown;
        public void OnChildKeyDown(Control child, KeyEventArgs e)
        {
            if (BubbleKeyDown != null)
            {
                BubbleKeyDown(new ControlBubbleEventArgs<KeyEventArgs>(child, e));
            }
        }

        public ControlBubbleEventHandler<KeyEventArgs> BubbleKeyUp;
        public void OnChildKeyUp(Control child, KeyEventArgs e)
        {
            if (BubbleKeyUp != null)
            {
                BubbleKeyUp(new ControlBubbleEventArgs<KeyEventArgs>(child, e));
            }
        }

        public ControlBubbleEventHandler<KeyPressEventArgs> BubbleKeyPress;
        public void OnChildKeyPress(Control child, KeyPressEventArgs e)
        {
            if (BubbleKeyPress != null)
            {
                BubbleKeyPress(new ControlBubbleEventArgs<KeyPressEventArgs>(child, e));
            }
        }

        #endregion

        #region EventAction

        private readonly EventHandler _childMouseEnter;
        private readonly EventHandler _childMouseLeave;
        private readonly MouseEventHandler _childMouseDown;
        private readonly MouseEventHandler _childMouseUp;
        private readonly MouseEventHandler _childMouseClick;
        private readonly MouseEventHandler _childMouseMove;
        private readonly MouseEventHandler _childMouseWheel;

        private readonly KeyEventHandler _childKeyDown;
        private readonly KeyEventHandler _childKeyUp;
        private readonly KeyPressEventHandler _childKeyPress;

        #endregion

        protected BaseControl()
        {
            _childMouseEnter = (sender, arg) => OnChildMouseEnter((Control)sender, arg);
            _childMouseLeave = (sender, arg) => OnChildMouseLeave((Control)sender, arg);
            _childMouseDown = (sender, arg) => OnChildMouseDown((Control)sender, arg);
            _childMouseUp = (sender, arg) => OnChildMouseUp((Control)sender, arg);
            _childMouseClick = (sender, arg) => OnChildMouseClick((Control)sender, arg);
            _childMouseMove = (sender, arg) => OnChildMouseMove((Control)sender, arg);
            _childMouseWheel = (sender, arg) => OnChildMouseWheel((Control)sender, arg);

            _childKeyDown = (sender, arg) => OnChildKeyDown((Control)sender, arg);
            _childKeyUp = (sender, arg) => OnChildKeyUp((Control)sender, arg);
            _childKeyPress = (sender, arg) => OnChildKeyPress((Control)sender, arg);

            this.ControlAdded += BaseControl_ControlAdded;
            this.ControlRemoved += BaseControl_ControlRemoved;
        }

        private void BaseControl_ControlAdded(object sender, ControlEventArgs e)
        {
            if (e == null || e.Control == null) return;

            var child = e.Control;
            if (child != null)
            {
                child.MouseEnter += _childMouseEnter;
                child.MouseLeave += _childMouseLeave;
                child.MouseDown += _childMouseDown;
                child.MouseUp += _childMouseUp;
                child.MouseClick += _childMouseClick;
                child.MouseMove += _childMouseMove;
                child.MouseWheel += _childMouseWheel;

                child.KeyDown += _childKeyDown;
                child.KeyUp += _childKeyUp;
                child.KeyPress += _childKeyPress;

                child.ControlAdded += BaseControl_ControlAdded;

                foreach (Control ctrl in child.Controls)
                {
                    BaseControl_ControlAdded(ctrl, new ControlEventArgs(ctrl));
                }
            }
        }

        private void BaseControl_ControlRemoved(object sender, ControlEventArgs e)
        {
            var child = e.Control;
            if (child != null)
            {
                child.MouseEnter -= _childMouseEnter;
                child.MouseLeave -= _childMouseLeave;
                child.MouseDown -= _childMouseDown;
                child.MouseUp -= _childMouseUp;
                child.MouseClick -= _childMouseClick;
                child.MouseMove -= _childMouseMove;
                child.MouseWheel -= _childMouseWheel;

                child.KeyDown -= _childKeyDown;
                child.KeyUp -= _childKeyUp;
                child.KeyPress -= _childKeyPress;

                child.ControlAdded -= BaseControl_ControlAdded;

                foreach (Control ctrl in child.Controls)
                {
                    BaseControl_ControlRemoved(ctrl, null);
                }

                child.ControlRemoved -= BaseControl_ControlRemoved;
            }
        }
    }

    public delegate void ControlBubbleEventHandler<T>(ControlBubbleEventArgs<T> args);

    public class ControlBubbleEventArgs<T> : EventArgs
    {
        public Control Source { get; private set; }

        public bool Handled { get; set; }

        public T Args { get; private set; }

        public ControlBubbleEventArgs(Control source, T args)
        {
            Args = args;
            Source = source;
        }
    }
}
