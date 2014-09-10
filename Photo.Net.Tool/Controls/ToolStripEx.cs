using System;
using System.Threading;
using System.Windows.Forms;
using Photo.Net.Base;

namespace Photo.Net.Tool.Controls
{
    /// <summary>
    /// This class adds on to the functionality provided in System.Windows.Forms.ToolStrip.
    /// </summary>
    /// <remarks>
    /// The first aggravating thing I found out about ToolStrip is that it does not "click through."
    /// If the form that is hosting a ToolStrip is not active and you click on a button in the tool
    /// strip, it sets focus to the form but does NOT click the button. This makes sense in many
    /// situations, but definitely not for Paint.NET.
    /// </remarks>
    public class ToolStripEx
        : ToolStrip
    {
        private bool _clickThrough = true;
        private bool _managedFocus = true;
        private static int _lockFocusCount = 0;

        public ToolStripEx()
        {
            var tspr = this.Renderer as ToolStripProfessionalRenderer;

            if (tspr != null)
            {
                tspr.ColorTable.UseSystemColors = true;
                tspr.RoundedEdges = false;
            }

            this.ImageScalingSize = new System.Drawing.Size(UserInterface.ScaleWidth(16), UserInterface.ScaleHeight(16));
        }

        /// <summary>
        /// Gets or sets whether the ToolStripEx honors item clicks when its containing form does
        /// not have input focus.
        /// </summary>
        /// <remarks>
        /// Default value is true, which is the opposite of the behavior provided by the base
        /// ToolStrip class.
        /// </remarks>
        public bool ClickThrough
        {
            get
            {
                return this._clickThrough;
            }

            set
            {
                this._clickThrough = value;
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (this._clickThrough)
            {
                UserInterface.ClickThroughWndProc(ref m);
            }
        }

        /// <summary>
        /// This event is raised when this toolstrip instance wishes to relinquish focus.
        /// </summary>
        public event EventHandler RelinquishFocus;

        private void OnRelinquishFocus()
        {
            if (RelinquishFocus != null)
            {
                RelinquishFocus(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Gets or sets whether the toolstrip manages focus.
        /// </summary>
        /// <remarks>
        /// If this is true, the toolstrip will capture focus when the mouse enters its client area. It will then
        /// relinquish focus (via the RelinquishFocus event) when the mouse leaves. It will not capture or
        /// attempt to relinquish focus if MenuStripEx.IsAnyMenuActive returns true.
        /// </remarks>
        public bool ManagedFocus
        {
            get
            {
                return this._managedFocus;
            }

            set
            {
                this._managedFocus = value;
            }
        }

        protected override void OnItemAdded(ToolStripItemEventArgs e)
        {
            var tscb = e.Item as ToolStripComboBox;
            var tstb = e.Item as ToolStripTextBox;

            if (tscb != null)
            {
                tscb.DropDownClosed += ComboBox_DropDownClosed;
                tscb.Enter += ComboBox_Enter;
                tscb.Leave += ComboBox_Leave;
            }
            else if (tstb != null)
            {
                tstb.Enter += TextBox_Enter;
                tstb.Leave += TextBox_Enter;
            }
            else
            {
                e.Item.MouseEnter += OnItemMouseEnter;
            }

            base.OnItemAdded(e);
        }

        private static void PushLockFocus()
        {
            Interlocked.Increment(ref _lockFocusCount);
        }

        private static void PopLockFocus()
        {
            Interlocked.Decrement(ref _lockFocusCount);
        }

        private static bool IsFocusLocked
        {
            get
            {
                return _lockFocusCount > 0;
            }
        }

        private void ComboBox_Enter(object sender, EventArgs e)
        {
            PushLockFocus();
        }

        private void ComboBox_Leave(object sender, EventArgs e)
        {
            PopLockFocus();
        }

        private void TextBox_Enter(object sender, EventArgs e)
        {
            PushLockFocus();
        }

        private void ComboBox_DropDownClosed(object sender, EventArgs e)
        {
            OnRelinquishFocus();
        }

        protected override void OnItemRemoved(ToolStripItemEventArgs e)
        {
            var tscb = e.Item as ToolStripComboBox;
            var tstb = e.Item as ToolStripTextBox;

            if (tscb != null)
            {
                tscb.DropDownClosed -= ComboBox_DropDownClosed;
                tscb.Enter -= ComboBox_Enter;
                tscb.Leave -= ComboBox_Leave;
            }
            else if (tstb != null)
            {
                tstb.Enter -= TextBox_Enter;
                tstb.Leave -= TextBox_Enter;
            }
            else
            {
                e.Item.MouseEnter -= OnItemMouseEnter;
            }

            base.OnItemRemoved(e);
        }

        private void OnItemMouseEnter(object sender, EventArgs e)
        {
            if (this._managedFocus && !MenuStripEx.IsAnyMenuActive && UserInterface.IsOurAppActive && !IsFocusLocked)
            {
                this.Focus();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (this._managedFocus && !MenuStripEx.IsAnyMenuActive && UserInterface.IsOurAppActive && !IsFocusLocked)
            {
                OnRelinquishFocus();
            }

            base.OnMouseLeave(e);
        }
    }
}
