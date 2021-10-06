namespace LEVCANsharpTest.paramControls
{
    partial class BaseControl
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
            if (disposing && this.linePen != null)
            {
                this.linePen.Dispose();
                this.linePen = null;
            }

            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.NameLabel = new System.Windows.Forms.Label();
            this.SplitCont = new System.Windows.Forms.SplitContainer();
            ((System.ComponentModel.ISupportInitialize)(this.SplitCont)).BeginInit();
            this.SplitCont.Panel1.SuspendLayout();
            this.SplitCont.SuspendLayout();
            this.SuspendLayout();
            // 
            // NameLabel
            // 
            this.NameLabel.BackColor = System.Drawing.Color.Transparent;
            this.NameLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.NameLabel.Location = new System.Drawing.Point(0, 0);
            this.NameLabel.Name = "NameLabel";
            this.NameLabel.Size = new System.Drawing.Size(180, 26);
            this.NameLabel.TabIndex = 1;
            this.NameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // SplitCont
            // 
            this.SplitCont.BackColor = System.Drawing.Color.Transparent;
            this.SplitCont.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SplitCont.Location = new System.Drawing.Point(0, 0);
            this.SplitCont.Name = "SplitCont";
            // 
            // SplitCont.Panel1
            // 
            this.SplitCont.Panel1.Controls.Add(this.NameLabel);
            this.SplitCont.Panel1MinSize = 75;
            this.SplitCont.Panel2MinSize = 75;
            this.SplitCont.Size = new System.Drawing.Size(300, 26);
            this.SplitCont.SplitterDistance = 180;
            this.SplitCont.SplitterWidth = 2;
            this.SplitCont.TabIndex = 3;
            this.SplitCont.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.splitContainer1_SplitterMoved);
            this.SplitCont.MouseDown += new System.Windows.Forms.MouseEventHandler(this.splitCont_MouseDown);
            this.SplitCont.MouseMove += new System.Windows.Forms.MouseEventHandler(this.splitCont_MouseMove);
            this.SplitCont.MouseUp += new System.Windows.Forms.MouseEventHandler(this.splitCont_MouseUp);
            // 
            // BaseControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLight;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this.SplitCont);
            this.DoubleBuffered = true;
            this.Margin = new System.Windows.Forms.Padding(1);
            this.MinimumSize = new System.Drawing.Size(100, 26);
            this.Name = "BaseControl";
            this.Size = new System.Drawing.Size(300, 26);
            this.SplitCont.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.SplitCont)).EndInit();
            this.SplitCont.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        public System.Windows.Forms.SplitContainer SplitCont;
        public System.Windows.Forms.Label NameLabel;
    }
}
