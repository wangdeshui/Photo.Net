using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Resources;
using System.Threading;
using System.Windows.Forms;
using Photo.Net.Base;
using Photo.Net.Base.Delegate;
using Photo.Net.Core;
using Photo.Net.Resource;
using Photo.Net.Tool.Core;
using Photo.Net.Tool.Events;
using WeifenLuo.WinFormsUI.Docking;

namespace Photo.Net.Tool.Window
{
    /// <summary>
    /// This Form class is used to fix a few bugs in Windows Forms, and to add a few performance
    /// enhancements, such as disabling opacity != 1.0 when running in a remote TS/RD session.
    /// We derive from this class instead of Windows.Forms.Form directly.
    /// </summary>
    public class BaseWindow
        : DockContent
    {
        static BaseWindow()
        {
            Application.EnterThreadModal += Application_EnterThreadModal;
            Application.LeaveThreadModal += Application_LeaveThreadModal;
            Application_EnterThreadModal(null, EventArgs.Empty);
        }

        // This set keeps track of forms which cannot be the current modal form.
        private static readonly Stack<List<Form>> ParentModalForms = new Stack<List<Form>>();

        private static bool IsInParentModalForms(Form form)
        {
            foreach (List<Form> formList in ParentModalForms)
            {
                foreach (Form parentModalForm in formList)
                {
                    if (parentModalForm == form)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static List<Form> GetAllPeerForms(Form form)
        {
            if (form == null)
            {
                return new List<Form>();
            }

            if (form.Owner != null)
            {
                return GetAllPeerForms(form.Owner);
            }

            var forms = new List<Form>();
            forms.Add(form);
            forms.AddRange(form.OwnedForms);

            return forms;
        }

        private static void Application_EnterThreadModal(object sender, EventArgs e)
        {
            Form activeForm = Form.ActiveForm;
            List<Form> allPeerForms = GetAllPeerForms(activeForm);
            ParentModalForms.Push(allPeerForms);
        }

        private static void Application_LeaveThreadModal(object sender, EventArgs e)
        {
            ParentModalForms.Pop();
        }

        protected override void OnShown(EventArgs e)
        {
            isShown = true;
            base.OnShown(e);
        }

        public bool IsShown
        {
            get
            {
                return this.isShown;
            }
        }

        private bool isShown = false;
        private bool enableOpacity = true;
        private double ourOpacity = 1.0; // store opacity setting so that when we go from disabled->enabled opacity we can set the correct value
        private System.ComponentModel.IContainer components;
        private bool instanceEnableOpacity = true;
        private static bool globalEnableOpacity = true;
        private readonly WindowEx _formEx;
        private bool processFormHotKeyMutex = false; // if we're already processing a form hotkey, don't let other hotkeys execute.

        private static Dictionary<Keys, Function<bool, Keys>> hotkeyRegistrar = null;

        /// <summary>
        /// Registers a form-wide hot key, and a callback for when the key is pressed.
        /// The callback must be an instance method on a Control. Whatever Form the Control
        /// is on will process the hotkey, as long as the Form is derived from PdnBaseForm.
        /// </summary>
        public static void RegisterFormHotKey(Keys keys, Function<bool, Keys> callback)
        {
            var targetAsComponent = callback.Target as IComponent;
            var targetAsHotKeyTarget = callback.Target as IHotKeyTarget;

            if (targetAsComponent == null && targetAsHotKeyTarget == null)
            {
                throw new ArgumentException("target instance must implement IComponent or IHotKeyTarget", "callback");
            }

            if (hotkeyRegistrar == null)
            {
                hotkeyRegistrar = new Dictionary<Keys, Function<bool, Keys>>();
            }

            Function<bool, Keys> theDelegate = null;

            if (hotkeyRegistrar.ContainsKey(keys))
            {
                theDelegate = hotkeyRegistrar[keys];
                theDelegate += callback;
                hotkeyRegistrar[keys] = theDelegate;
            }
            else
            {
                theDelegate = new Function<bool, Keys>(callback);
                hotkeyRegistrar.Add(keys, theDelegate);
            }

            if (targetAsComponent != null)
            {
                targetAsComponent.Disposed += TargetAsComponent_Disposed;
            }
            else
            {
                targetAsHotKeyTarget.Disposed += TargetAsHotKeyTarget_Disposed;
            }
        }

        private bool ShouldProcessHotKey(Keys keys)
        {
            Keys keyOnly = keys & ~Keys.Modifiers;

            if (keyOnly == Keys.Back ||
                keyOnly == Keys.Delete ||
                keyOnly == Keys.Left ||
                keyOnly == Keys.Right ||
                keyOnly == Keys.Up ||
                keyOnly == Keys.Down ||
                keys == (Keys.Control | Keys.A) ||        // select all
                keys == (Keys.Control | Keys.Z) ||        // undo
                keys == (Keys.Control | Keys.Y) ||        // redo
                keys == (Keys.Control | Keys.X) ||        // cut
                keys == (Keys.Control | Keys.C) ||        // copy
                keys == (Keys.Control | Keys.V) ||        // paste
                keys == (Keys.Shift | Keys.Delete) ||     // cut (left-handed)
                keys == (Keys.Control | Keys.Insert) ||   // copy (left-handed)
                keys == (Keys.Shift | Keys.Insert)        // paste (left-handed)
                )
            {
                var focusedCtrl = Utility.FindFocus();

                if (focusedCtrl is TextBox || focusedCtrl is ComboBox || focusedCtrl is UpDownBase)
                {
                    return false;
                }
            }

            return true;
        }

        private static void TargetAsComponent_Disposed(object sender, EventArgs e)
        {
            ((IComponent)sender).Disposed -= TargetAsComponent_Disposed;
            RemoveDisposedTarget(sender);
        }

        private static void TargetAsHotKeyTarget_Disposed(object sender, EventArgs e)
        {
            ((IHotKeyTarget)sender).Disposed -= TargetAsHotKeyTarget_Disposed;
            RemoveDisposedTarget(sender);
        }

        static void RemoveDisposedTarget(object sender)
        {
            // Control was disposed, but it never unregistered for its hotkeys!
            var keysList = new List<Keys>(hotkeyRegistrar.Keys);

            foreach (Keys keys in keysList)
            {
                Function<bool, Keys> theMultiDelegate = hotkeyRegistrar[keys];

                foreach (Delegate theDelegate in theMultiDelegate.GetInvocationList())
                {
                    if (object.ReferenceEquals(theDelegate.Target, sender))
                    {
                        UnregisterFormHotKey(keys, (Function<bool, Keys>)theDelegate);
                    }
                }
            }
        }

        public static void UnregisterFormHotKey(Keys keys, Function<bool, Keys> callback)
        {
            if (hotkeyRegistrar != null)
            {
                Function<bool, Keys> theDelegate = hotkeyRegistrar[keys];
                theDelegate -= callback;
                hotkeyRegistrar[keys] = theDelegate;

                var targetAsComponent = callback.Target as IComponent;
                if (targetAsComponent != null)
                {
                    targetAsComponent.Disposed -= TargetAsComponent_Disposed;
                }

                var targetAsHotKeyTarget = callback.Target as IHotKeyTarget;
                if (targetAsHotKeyTarget != null)
                {
                    targetAsHotKeyTarget.Disposed -= TargetAsHotKeyTarget_Disposed;
                }

                if (theDelegate.GetInvocationList().Length == 0)
                {
                    hotkeyRegistrar.Remove(keys);
                }

                if (hotkeyRegistrar.Count == 0)
                {
                    hotkeyRegistrar = null;
                }
            }
        }

        public void Flash()
        {
            UserInterface.FlashForm(this);
        }

        public void RestoreWindow()
        {
            if (WindowState == FormWindowState.Minimized)
            {
                UserInterface.RestoreWindow(this);
            }
        }

        /// <summary>
        /// Returns the currently active modal form if the process is in the foreground and is active.
        /// </summary>
        /// <remarks>
        /// If Form.ActiveForm is modeless, we search up the chain of owner forms
        /// to find its modeless owner form.
        /// </remarks>
        public static Form CurrentModalForm
        {
            get
            {
                Form theForm = Form.ActiveForm;

                while (theForm != null && !theForm.Modal && theForm.Owner != null)
                {
                    theForm = theForm.Owner;
                }

                return theForm;
            }
        }

        /// <summary>
        /// Gets whether the current form is the processes' top level modal form.
        /// </summary>
        public bool IsCurrentModalForm
        {
            get
            {
                if (IsInParentModalForms(this))
                {
                    return false;
                }

                if (this.ContainsFocus)
                {
                    return true;
                }

                foreach (Form ownedForm in this.OwnedForms)
                {
                    if (ownedForm.ContainsFocus)
                    {
                        return true;
                    }
                }

                return (this == CurrentModalForm);
            }
        }

        private bool IsTargetFormActive(object target)
        {
            Control targetControl = null;

            var asControl = target as Control;

            if (asControl != null)
            {
                targetControl = asControl;
            }

            if (targetControl == null)
            {
                var asIFormAssociate = target as IFormAssociate;

                if (asIFormAssociate != null)
                {
                    targetControl = asIFormAssociate.AssociatedForm;
                }
            }

            // target is not a control, or a type of non-control that we recognize as hosted by a control
            if (targetControl == null)
            {
                return false;
            }

            Form targetForm = targetControl.FindForm();

            // target is not on a form
            if (targetForm == null)
            {
                return false;
            }

            // is the target on the currently active form?
            Form activeModalForm = CurrentModalForm;

            if (targetForm == activeModalForm)
            {
                return true;
            }

            // Nope.
            return false;
        }

        private static object GetConcreteTarget(object target)
        {
            var asDelegate = target as Delegate;

            if (asDelegate == null)
            {
                return target;
            }
            else
            {
                return GetConcreteTarget(asDelegate.Target);
            }
        }

        private bool ProcessFormHotKey(Keys keyData)
        {
            bool processed = false;

            if (this.processFormHotKeyMutex)
            {
                processed = true;
            }
            else
            {
                this.processFormHotKeyMutex = true;

                try
                {
                    if (hotkeyRegistrar != null && hotkeyRegistrar.ContainsKey(keyData))
                    {
                        Function<bool, Keys> theDelegate = hotkeyRegistrar[keyData];
                        Delegate[] invokeList = theDelegate.GetInvocationList();

                        for (int i = invokeList.Length - 1; i >= 0; --i)
                        {
                            var invokeMe = (Function<bool, Keys>)invokeList[i];
                            object concreteTarget = GetConcreteTarget(invokeMe.Target);

                            if (IsTargetFormActive(concreteTarget))
                            {
                                bool result = invokeMe(keyData);

                                if (result)
                                {
                                    // The callback handled the key.
                                    processed = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                finally
                {
                    this.processFormHotKeyMutex = false;
                }
            }

            return processed;
        }

        private void OnProcessCmdKeyRelay(object sender, WindowEx.ProcessCmdKeyEventArgs e)
        {
            bool handled = e.Handled;

            if (!handled)
            {
                handled = ProcessCmdKeyData(e.KeyData);
                e.Handled = handled;
            }
        }

        public bool RelayProcessCmdKey(ref Message msg, Keys keyData)
        {
            return ProcessCmdKeyData(keyData);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            bool processed = ProcessCmdKeyData(keyData);

            if (!processed)
            {
                processed = base.ProcessCmdKey(ref msg, keyData);
            }

            return processed;
        }

        private bool ProcessCmdKeyData(Keys keyData)
        {
            bool shouldHandle = ShouldProcessHotKey(keyData);

            if (shouldHandle)
            {
                bool processed = ProcessFormHotKey(keyData);
                return processed;
            }
            else
            {
                return false;
            }
        }

        public static void UpdateAllForms()
        {
            try
            {
                foreach (Form form in Application.OpenForms)
                {
                    try
                    {
                        form.Update();
                    }

                    catch (Exception)
                    {
                    }
                }
            }

            catch (InvalidOperationException)
            {
            }
        }

        public static EventHandler EnableOpacityChanged;
        private static void OnEnableOpacityChanged()
        {
            if (EnableOpacityChanged != null)
            {
                EnableOpacityChanged(null, EventArgs.Empty);
            }
        }

        public bool EnableInstanceOpacity
        {
            get
            {
                return instanceEnableOpacity;
            }

            set
            {
                instanceEnableOpacity = value;
                this.DecideOpacitySetting();
            }
        }

        /// <summary>
        /// Gets or sets a flag that enables or disables opacity for all PdnBaseForm instances.
        /// If a particular form's EnableInstanceOpacity property is false, that will override
        /// this property being 'true'.
        /// </summary>
        public static bool EnableOpacity
        {
            get
            {
                return globalEnableOpacity;
            }

            set
            {
                globalEnableOpacity = value;
                OnEnableOpacityChanged();
            }
        }

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
                return this._formEx.ForceActiveTitleBar;
            }

            set
            {
                this._formEx.ForceActiveTitleBar = value;
            }
        }

        private ThreadPriority originalPriority;

        protected override void OnScroll(ScrollEventArgs se)
        {
            Thread.CurrentThread.Priority = this.originalPriority;
            base.OnScroll(se);
        }

        public BaseWindow()
        {
            this.originalPriority = Thread.CurrentThread.Priority;
            Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;

            UserInterface.InitScaling(this);

            this.SuspendLayout();
            InitializeComponent();

            this._formEx = new WindowEx(this, new RealParentWndProcDelegate(this.RealWndProc));
            this.Controls.Add(this._formEx);
            this._formEx.Visible = false;
            DecideOpacitySetting();
            this.ResumeLayout(false);

            this._formEx.ProcessCmdKeyRelay += OnProcessCmdKeyRelay;
        }

        protected override void OnLoad(EventArgs e)
        {
            if (!this.DesignMode)
            {
                LoadResources();
            }

            base.OnLoad(e);
        }

        public virtual void LoadResources()
        {
            if (!this.DesignMode)
            {
                string stringName = this.Name + ".Localized";
                //                string stringValue = StringsResourceManager.GetString(stringName);
                //
                //                if (stringValue != null)
                //                {
                //                    try
                //                    {
                //                        bool boolValue = bool.Parse(stringValue);
                //
                //                        if (boolValue)
                //                        {
                //                            //                            LoadLocalizedResources();
                //                        }
                //                    }
                //
                //                    catch (Exception)
                //                    {
                //                    }
                //                }
            }
        }

        protected virtual ResourceManager StringsResourceManager
        {
            get
            {
                return PdnResources.Strings;
            }
        }

        //        private void LoadLocalizedResources()
        //        {
        //            LoadLocalizedResources(this.Name, this);
        //        }
        //
        //
        //        private void LoadLocalizedResources(string baseName, Tool control)
        //        {
        //            // Text
        //            string textStringName = baseName + ".Text";
        //            string textString = this.StringsResourceManager.GetString(textStringName);
        //
        //            if (textString != null)
        //            {
        //                control.Text = textString;
        //            }
        //
        //            // Location
        //            string locationStringName = baseName + ".Location";
        //            string locationString = this.StringsResourceManager.GetString(locationStringName);
        //
        //            if (locationString != null)
        //            {
        //                try
        //                {
        //                    int x;
        //                    int y;
        //
        //                    ParsePair(locationString, out x, out y);
        //                    control.Location = new Point(x, y);
        //                }
        //
        //                catch (Exception ex)
        //                {
        //                }
        //            }
        //
        //            // Size
        //            string sizeStringName = baseName + ".Size";
        //            string sizeString = this.StringsResourceManager.GetString(sizeStringName);
        //
        //            if (sizeString != null)
        //            {
        //                try
        //                {
        //                    int width;
        //                    int height;
        //
        //                    ParsePair(sizeString, out width, out height);
        //                    control.Size = new Size(width, height);
        //                }
        //
        //                catch (Exception ex)
        //                {
        //                }
        //            }
        //
        //            // Recurse
        //            foreach (Control child in control.Controls)
        //            {
        //                if (child.Name == null || child.Name.Length > 0)
        //                {
        //                    string newBaseName = baseName + "." + child.Name;
        //                    LoadLocalizedResources(newBaseName, child);
        //                }
        //            }
        //        }

        private void ParsePair(string theString, out int x, out int y)
        {
            string[] split = theString.Split(',');
            x = int.Parse(split[0]);
            y = int.Parse(split[1]);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (!e.Cancel)
            {
                this.ForceActiveTitleBar = false;
            }
        }

        private void EnableOpacityChangedHandler(object sender, EventArgs e)
        {
            DecideOpacitySetting();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            EnableOpacityChanged += EnableOpacityChangedHandler;
            UserSessions.SessionChanged += new EventHandler(UserSessions_SessionChanged);
            DecideOpacitySetting();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            base.OnHandleDestroyed(e);

            EnableOpacityChanged -= EnableOpacityChangedHandler;
            UserSessions.SessionChanged -= new EventHandler(UserSessions_SessionChanged);
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                    components = null;
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Sets the opacity of the form.
        /// </summary>
        /// <param name="newOpacity">The new opacity value.</param>
        /// <remarks>
        /// Depending on the system configuration, this request may be ignored. For example,
        /// when running within a Terminal Service (or Remote Desktop) session, opacity will
        /// always be set to 1.0 for performance reasons.
        /// </remarks>
        public new double Opacity
        {
            get
            {
                return this.ourOpacity;
            }

            set
            {
                if (enableOpacity)
                {
                    // Bypassing Form.Opacity eliminates a "black flickering" that occurs when
                    // the form transitions from Opacity=1.0 to Opacity != 1.0, or vice versa.
                    // It appears to be a result of toggling the WS_EX_LAYERED style, or the
                    // fact that Form.Opacity re-applies visual styles when this value transition
                    // takes place.
                    UserInterface.SetFormOpacity(this, value);
                }

                this.ourOpacity = value;
            }
        }

        /// <summary>
        /// Decides whether or not to have opacity be enabled.
        /// </summary>
        private void DecideOpacitySetting()
        {
            if (UserSessions.IsRemote || !BaseWindow.globalEnableOpacity || !this.EnableInstanceOpacity)
            {
                if (this.enableOpacity)
                {
                    try
                    {
                        UserInterface.SetFormOpacity(this, 1.0);
                    }

                    // This fails in certain odd situations (bug #746), so we just eat the exception.
                    catch (System.ComponentModel.Win32Exception)
                    {
                    }
                }

                this.enableOpacity = false;
            }
            else
            {
                if (!this.enableOpacity)
                {
                    // This fails in certain odd situations (bug #746), so we just eat the exception.
                    try
                    {
                        UserInterface.SetFormOpacity(this, this.ourOpacity);
                    }

                    catch (Win32Exception)
                    {
                    }
                }

                this.enableOpacity = true;
            }
        }

        public double ScreenAspect
        {
            get
            {
                Rectangle bounds = System.Windows.Forms.Screen.FromControl(this).Bounds;
                double aspect = (double)bounds.Width / (double)bounds.Height;
                return aspect;
            }
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.SuspendLayout();
            // 
            // PdnBaseForm
            // 
            this.AutoScaleDimensions = new SizeF(96F, 96F);
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(291, 270);
            this.Name = "PdnBaseForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "PdnBaseForm";
            this.ResumeLayout(false);

        }
        #endregion

        public event MovingEventHandler Moving;
        protected virtual void OnMoving(MovingEventArgs mea)
        {
            if (Moving != null)
            {
                Moving(this, mea);
            }
        }

        public event CancelEventHandler QueryEndSession;
        protected virtual void OnQueryEndSession(CancelEventArgs e)
        {
            if (QueryEndSession != null)
            {
                QueryEndSession(this, e);
            }
        }

        private void UserSessions_SessionChanged(object sender, EventArgs e)
        {
            this.DecideOpacitySetting();
        }

        void RealWndProc(ref Message m)
        {
            OurWndProc(ref m);
        }

        protected override void WndProc(ref Message m)
        {
            if (this._formEx == null)
            {
                base.WndProc(ref m);
            }
            else if (!this._formEx.HandleParentWndProc(ref m))
            {
                OurWndProc(ref m);
            }
        }

        private void OurWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case 0x0216: // WM_MOVING
                    unsafe
                    {
                        int* p = (int*)m.LParam;
                        Rectangle rect = Rectangle.FromLTRB(p[0], p[1], p[2], p[3]);

                        var mea = new MovingEventArgs(rect);
                        OnMoving(mea);

                        p[0] = mea.Rectangle.Left;
                        p[1] = mea.Rectangle.Top;
                        p[2] = mea.Rectangle.Right;
                        p[3] = mea.Rectangle.Bottom;

                        m.Result = new IntPtr(1);
                    }
                    break;

                // WinForms doesn't handle this message correctly and wrongly returns 0 instead of 1.
                case 0x0011: // WM_QUERYENDSESSION
                    CancelEventArgs e = new CancelEventArgs();
                    OnQueryEndSession(e);
                    m.Result = e.Cancel ? IntPtr.Zero : new IntPtr(1);
                    break;

                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        public Size ClientSizeToWindowSize(Size clientSize)
        {
            Size baseClientSize = ClientSize;
            Size baseWindowSize = Size;

            int extraWidth = baseWindowSize.Width - baseClientSize.Width;
            int extraHeight = baseWindowSize.Height - baseClientSize.Height;

            Size windowSize = new Size(clientSize.Width + extraWidth, clientSize.Height + extraHeight);
            return windowSize;
        }

        public Size WindowSizeToClientSize(Size windowSize)
        {
            Size baseClientSize = ClientSize;
            Size baseWindowSize = Size;

            int extraWidth = baseWindowSize.Width - baseClientSize.Width;
            int extraHeight = baseWindowSize.Height - baseClientSize.Height;
            Size clientSize = new Size(windowSize.Width - extraWidth, windowSize.Height - extraHeight);

            return clientSize;
        }

        public Rectangle ClientBoundsToWindowBounds(Rectangle clientBounds)
        {
            Rectangle currentBounds = this.Bounds;
            Rectangle currentClientBounds = this.RectangleToScreen(ClientRectangle);

            Rectangle newWindowBounds = new Rectangle(
                clientBounds.Left - (currentClientBounds.Left - currentBounds.Left),
                clientBounds.Top - (currentClientBounds.Top - currentBounds.Top),
                clientBounds.Width + (currentBounds.Width - currentClientBounds.Width),
                clientBounds.Height + (currentBounds.Height - currentClientBounds.Height));

            return newWindowBounds;
        }

        public Rectangle WindowBoundsToClientBounds(Rectangle windowBounds)
        {
            Rectangle currentBounds = this.Bounds;
            Rectangle currentClientBounds = this.RectangleToScreen(ClientRectangle);

            Rectangle newClientBounds = new Rectangle(
                windowBounds.Left + (currentClientBounds.Left - currentBounds.Left),
                windowBounds.Top + (currentClientBounds.Top - currentBounds.Top),
                windowBounds.Width - (currentBounds.Width - currentClientBounds.Width),
                windowBounds.Height - (currentBounds.Height - currentClientBounds.Height));

            return newClientBounds;
        }

        public void EnsureFormIsOnScreen()
        {
            if (this.WindowState == FormWindowState.Maximized)
            {
                return;
            }

            if (this.WindowState == FormWindowState.Minimized)
            {
                return;
            }

            Screen ourScreen;

            try
            {
                ourScreen = Screen.FromControl(this);
            }

            catch (Exception)
            {
                ourScreen = null;
            }

            if (ourScreen == null)
            {
                ourScreen = Screen.PrimaryScreen;
            }

            Rectangle currentBounds = Bounds;
            Rectangle newBounds = EnsureRectIsOnScreen(ourScreen, currentBounds);
            Bounds = newBounds;
        }

        public static Rectangle EnsureRectIsOnScreen(Screen screen, Rectangle bounds)
        {
            Rectangle newBounds = bounds;
            Rectangle screenBounds = screen.WorkingArea;

            // Make sure the bottom and right do not fall off the edge, by moving the bounds
            if (newBounds.Right > screenBounds.Right)
            {
                newBounds.X -= (newBounds.Right - screenBounds.Right);
            }

            if (newBounds.Bottom > screenBounds.Bottom)
            {
                newBounds.Y -= (newBounds.Bottom - screenBounds.Bottom);
            }

            // Make sure the top and left haven't fallen off, by moving
            if (newBounds.Left < screenBounds.Left)
            {
                newBounds.X = screenBounds.Left;
            }

            if (newBounds.Top < screenBounds.Top)
            {
                newBounds.Y = screenBounds.Top;
            }

            // Make sure that we are not too wide / tall, by resizing
            if (newBounds.Right > screenBounds.Right)
            {
                newBounds.Width -= (newBounds.Right - screenBounds.Right);
            }

            if (newBounds.Bottom > screenBounds.Bottom)
            {
                newBounds.Height -= (newBounds.Bottom - screenBounds.Bottom);
            }

            // All done.
            return newBounds;
        }
    }
}
