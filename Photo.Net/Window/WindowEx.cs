using System;
using System.Windows.Forms;
using Photo.Net.Base;
using Photo.Net.Base.NativeWrapper;
using Photo.Net.Events;

namespace Photo.Net.Window
{
    /// <summary>
    /// Provides special methods and properties that must be implemented in a
    /// system-specific manner. It is implemented as an object that is hosted
    /// by the PdnBaseForm class. This way there is no inheritance hierarchy 
    /// extending into the SystemLayer assembly.
    /// </summary>
    public sealed class WindowEx
        : Control
    {
        private Form host;
        private readonly RealParentWndProcDelegate _realParentWndProc;
        private bool _forceActiveTitleBar;

        /// <summary>
        /// Gets or sets the titlebar rendering behavior for when the form is deactivated.
        /// </summary>
        /// <remarks>
        /// If this property is false, the titlebar will be rendered in a different color when the form
        /// is inactive as opposed to active. If this property is true, it will always render with the
        /// active style. If the whole application is deactivated, the title bar will still be drawn in
        /// an inactive state.
        /// </remarks>
        public bool ForceActiveTitleBar
        {
            get
            {
                return this._forceActiveTitleBar;
            }

            set
            {
                this._forceActiveTitleBar = value;
            }
        }

        public WindowEx(Form host, RealParentWndProcDelegate realParentWndProc)
        {
            this.host = host;
            this._realParentWndProc = realParentWndProc;
        }

        public class ProcessCmdKeyEventArgs
            : EventArgs
        {
            private bool handled;
            public bool Handled
            {
                get
                {
                    return this.handled;
                }

                set
                {
                    this.handled = value;
                }
            }

            private Keys keyData;
            public Keys KeyData
            {
                get
                {
                    return this.keyData;
                }
            }

            public ProcessCmdKeyEventArgs(Keys keyData, bool handled)
            {
                this.keyData = keyData;
                this.handled = handled;
            }
        }

        public event EventHandler<ProcessCmdKeyEventArgs> ProcessCmdKeyRelay;

        public bool RelayProcessCmdKey(Keys keyData)
        {
            bool handled = false;

            if (ProcessCmdKeyRelay != null)
            {
                ProcessCmdKeyEventArgs e = new ProcessCmdKeyEventArgs(keyData, false);
                ProcessCmdKeyRelay(this, e);
                handled = e.Handled;
            }

            return handled;
        }

        internal static WindowEx FindFormEx(Form host)
        {
            if (host != null)
            {
                Control.ControlCollection controls = host.Controls;

                for (int i = 0; i < controls.Count; ++i)
                {
                    WindowEx formEx = controls[i] as WindowEx;

                    if (formEx != null)
                    {
                        return formEx;
                    }
                }
            }

            return null;
        }

        private int ignoreNcActivate = 0;

        /// <summary>
        /// Manages some special handling of window messages.
        /// </summary>
        /// <param name="m"></param>
        /// <returns>true if the message was handled, false if the caller should handle the message.</returns>
        public bool HandleParentWndProc(ref Message m)
        {
            bool returnVal = true;

            switch (m.Msg)
            {
                case NativeConstants.WM_NCPAINT:
                    goto default;

                case NativeConstants.WM_NCACTIVATE:
                    if (this._forceActiveTitleBar && m.WParam == IntPtr.Zero)
                    {
                        if (ignoreNcActivate > 0)
                        {
                            --ignoreNcActivate;
                            goto default;
                        }
                        else if (Form.ActiveForm != this.host ||  // Gets rid of: if you have the form active, then click on the desktop --> desktop refreshes
                                 !this.host.Visible)              // Gets rid of: desktop refresh on exit
                        {
                            goto default;
                        }
                        else
                        {
                            // Only 'lock' for the topmost form in the application. Otherwise you get the whole system
                            // refreshing (i.e. the dreaded "repaint the whole desktop 5 times" glitch) when you do things
                            // like minimize the window
                            // And only lock if we aren't minimized. Otherwise the desktop refreshes.
                            bool locked = false;
                            if (this.host.Owner == null &&
                                this.host.WindowState != FormWindowState.Minimized)
                            {
                                //UI.SetControlRedraw(this.host, false);
                                locked = true;
                            }

                            this._realParentWndProc(ref m);

                            SafeNativeMethods.SendMessageW(this.host.Handle, NativeConstants.WM_NCACTIVATE,
                                new IntPtr(1), IntPtr.Zero);

                            if (locked)
                            {
                                //UI.SetControlRedraw(this.host, true);
                                //this.host.Invalidate(true);
                            }

                            break;
                        }
                    }
                    else
                    {
                        goto default;
                    }

                case NativeConstants.WM_ACTIVATE:
                    goto default;

                case NativeConstants.WM_ACTIVATEAPP:
                    this._realParentWndProc(ref m);

                    // Check if the app is being deactivated
                    if (this._forceActiveTitleBar && m.WParam == IntPtr.Zero)
                    {
                        // If so, put our titlebar in the inactive state
                        SafeNativeMethods.PostMessageW(this.host.Handle, NativeConstants.WM_NCACTIVATE,
                            IntPtr.Zero, IntPtr.Zero);

                        ++ignoreNcActivate;
                    }

                    if (m.WParam == new IntPtr(1))
                    {
                        foreach (Form childForm in this.host.OwnedForms)
                        {
                            WindowEx childFormEx = FindFormEx(childForm);

                            if (childFormEx != null)
                            {
                                if (childFormEx.ForceActiveTitleBar && childForm.IsHandleCreated)
                                {
                                    SafeNativeMethods.PostMessageW(childForm.Handle, NativeConstants.WM_NCACTIVATE,
                                        new IntPtr(1), IntPtr.Zero);
                                }
                            }
                        }

                        WindowEx ownerEx = FindFormEx(this.host.Owner);
                        if (ownerEx != null)
                        {
                            if (ownerEx.ForceActiveTitleBar && this.host.Owner.IsHandleCreated)
                            {
                                SafeNativeMethods.PostMessageW(this.host.Owner.Handle, NativeConstants.WM_NCACTIVATE,
                                    new IntPtr(1), IntPtr.Zero);
                            }
                        }
                    }

                    break;

                default:
                    returnVal = false;
                    break;
            }

            GC.KeepAlive(this.host);
            return returnVal;
        }
    }
}
