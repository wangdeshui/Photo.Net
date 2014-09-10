namespace Photo.Net.Tool.Tools
{
    partial class ToolSet
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        public void InitializeComponent()
        {
            this.Tools = new ToolsControl();
            this.SuspendLayout();
            // 
            // toolsControl
            // 
            this.Tools.Location = new System.Drawing.Point(0, 0);
            this.Tools.Name = "toolsControl";
            this.Tools.Size = new System.Drawing.Size(50, 88);
            this.Tools.TabIndex = 0;
            // 
            // MainToolBarForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(50, 273);
            this.Controls.Add(this.Tools);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "ToolsForm";
            this.Controls.SetChildIndex(this.Tools, 0);
            this.ResumeLayout(false);
        }
    }
}