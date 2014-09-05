using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Photo.Net.Core;
using Photo.Net.Resource;
using Photo.Net.Tool.Controls;
using Photo.Net.Tool.Layer;
using Photo.Net.Tool.Window;

namespace Photo.Net.Forms
{
    public class LayerPropertiesDialog
        : BaseWindow
    {
        protected CheckBox VisibleCheckBox;
        protected Label NameLabel;
        protected TextBox NameBox;
        protected Button cancelButton;
        protected Button OkButton;
        private Container _components;
        private HeaderLabel _generalHeader;

        private ImageLayer _layer;

        [Browsable(false)]
        public ImageLayer Layer
        {
            get
            {
                return _layer;
            }

            set
            {
                this._layer = value;
                this._layer.SaveProperties();
                InitDialogFromLayer();
            }
        }

        protected virtual void InitLayerFromDialog()
        {
            this._layer.Name = this.NameBox.Text;
            this._layer.Visible = this.VisibleCheckBox.Checked;

            if (this.Owner != null)
            {
                this.Owner.Update();
            }
        }

        protected virtual void InitDialogFromLayer()
        {
            this.NameBox.Text = this._layer.Name;
            this.VisibleCheckBox.Checked = this._layer.Visible;
        }

        public LayerPropertiesDialog()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            this.Icon = Utility.ImageToIcon(PdnResources.GetImage("Icons.MenuLayersLayerPropertiesIcon.png"), Color.FromArgb(192, 192, 192));

            this.Text = PdnResources.GetString("LayerPropertiesDialog.Text");
            this.VisibleCheckBox.Text = PdnResources.GetString("LayerPropertiesDialog.VisibleCheckBox.Text");
            this.NameLabel.Text = PdnResources.GetString("LayerPropertiesDialog.NameLabel.Text");
            this._generalHeader.Text = PdnResources.GetString("LayerPropertiesDialog.GeneralHeader.Text");
            this.cancelButton.Text = PdnResources.GetString("Form.CancelButton.Text");
            this.OkButton.Text = PdnResources.GetString("Form.OkButton.Text");
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_components != null)
                {
                    _components.Dispose();
                    _components = null;
                }
            }

            base.Dispose(disposing);
        }

        protected override void OnLoad(EventArgs e)
        {
            this.NameBox.Select();
            this.NameBox.Select(0, this.NameBox.Text.Length);
            base.OnLoad(e);
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.VisibleCheckBox = new System.Windows.Forms.CheckBox();
            this.NameBox = new System.Windows.Forms.TextBox();
            this.NameLabel = new System.Windows.Forms.Label();
            this.cancelButton = new System.Windows.Forms.Button();
            this.OkButton = new System.Windows.Forms.Button();
            this._generalHeader = new HeaderLabel();
            this.SuspendLayout();
            // 
            // visibleCheckBox
            // 
            this.VisibleCheckBox.Location = new System.Drawing.Point(14, 43);
            this.VisibleCheckBox.Name = "VisibleCheckBox";
            this.VisibleCheckBox.Size = new System.Drawing.Size(90, 16);
            this.VisibleCheckBox.TabIndex = 3;
            this.VisibleCheckBox.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            this.VisibleCheckBox.FlatStyle = FlatStyle.System;
            this.VisibleCheckBox.CheckedChanged += new System.EventHandler(this.VisibleCheckBox_CheckedChanged);
            // 
            // nameBox
            // 
            this.NameBox.Location = new System.Drawing.Point(64, 24);
            this.NameBox.Name = "NameBox";
            this.NameBox.Size = new System.Drawing.Size(200, 20);
            this.NameBox.TabIndex = 2;
            this.NameBox.Text = "";
            this.NameBox.Enter += new System.EventHandler(this.NameBox_Enter);
            // 
            // nameLabel
            // 
            this.NameLabel.Location = new System.Drawing.Point(6, 24);
            this.NameLabel.Name = "NameLabel";
            this.NameLabel.Size = new System.Drawing.Size(50, 16);
            this.NameLabel.TabIndex = 2;
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.cancelButton.Location = new System.Drawing.Point(194, 69);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.TabIndex = 1;
            this.cancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // okButton
            // 
            this.OkButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OkButton.Location = new System.Drawing.Point(114, 69);
            this.OkButton.Name = "OkButton";
            this.OkButton.TabIndex = 0;
            this.OkButton.FlatStyle = FlatStyle.System;
            this.OkButton.Click += new System.EventHandler(this.OkButton_Click);
            // 
            // generalHeader
            // 
            this._generalHeader.Location = new System.Drawing.Point(6, 8);
            this._generalHeader.Name = "_generalHeader";
            this._generalHeader.Size = new System.Drawing.Size(269, 14);
            this._generalHeader.TabIndex = 4;
            this._generalHeader.TabStop = false;
            // 
            // LayerPropertiesDialog
            // 
            this.AcceptButton = this.OkButton;
            this.AutoScaleDimensions = new SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(274, 96);
            this.ControlBox = true;
            this.Controls.Add(this._generalHeader);
            this.Controls.Add(this.OkButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.NameBox);
            this.Controls.Add(this.VisibleCheckBox);
            this.Controls.Add(this.NameLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "LayerPropertiesDialog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Controls.SetChildIndex(this.NameLabel, 0);
            this.Controls.SetChildIndex(this.VisibleCheckBox, 0);
            this.Controls.SetChildIndex(this.NameBox, 0);
            this.Controls.SetChildIndex(this.cancelButton, 0);
            this.Controls.SetChildIndex(this.OkButton, 0);
            this.Controls.SetChildIndex(this._generalHeader, 0);
            this.ResumeLayout(false);

        }
        #endregion

        private void NameBox_Enter(object sender, System.EventArgs e)
        {
            this.NameBox.Select(0, NameBox.Text.Length);
        }

        private void OkButton_Click(object sender, System.EventArgs e)
        {
            DialogResult = DialogResult.OK;

            //            using (new WaitCursorChanger(this))
            //            {
            //                this.layer.PushSuppressPropertyChanged();
            //                InitLayerFromDialog();
            //                object currentProperties = this.layer.SaveProperties();
            //                this.layer.LoadProperties(this.originalProperties);
            //                this.layer.PopSuppressPropertyChanged();
            //
            //                this.layer.LoadProperties(currentProperties);
            //                this.originalProperties = layer.SaveProperties();
            //                //layer.Invalidate(); // no need to call Invalidate() -- it will be called by OnClosed()
            //            }

            Close();
        }

        private void CancelButton_Click(object sender, System.EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            //            using (new WaitCursorChanger(this))
            //            {
            //                this.layer.PushSuppressPropertyChanged();
            //                this.layer.LoadProperties(this.originalProperties);
            //                this.layer.PopSuppressPropertyChanged();
            //                this.layer.Invalidate();
            //            }

            base.OnClosed(e);
        }

        private void VisibleCheckBox_CheckedChanged(object sender, System.EventArgs e)
        {
            Layer.PushSuppressPropertyChanged();
            Layer.Visible = VisibleCheckBox.Checked;
            Layer.PopSuppressPropertyChanged();
        }
    }
}
