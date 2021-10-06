namespace LEVCANsharpTest.paramControls
{
    partial class ValueControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.ValueText = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.SplitCont)).BeginInit();
            this.SplitCont.Panel1.SuspendLayout();
            this.SplitCont.Panel2.SuspendLayout();
            this.SplitCont.SuspendLayout();
            this.SuspendLayout();
            // 
            // SplitCont
            // 
            // 
            // SplitCont.Panel2
            // 
            this.SplitCont.Panel2.Controls.Add(this.ValueText);
            this.SplitCont.Size = new System.Drawing.Size(298, 24);
            this.SplitCont.SplitterDistance = 178;
            // 
            // NameLabel
            // 
            this.NameLabel.Size = new System.Drawing.Size(178, 24);
            // 
            // ValueText
            // 
            this.ValueText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ValueText.Location = new System.Drawing.Point(0, 0);
            this.ValueText.MaxLength = 256;
            this.ValueText.Name = "ValueText";
            this.ValueText.Size = new System.Drawing.Size(118, 20);
            this.ValueText.TabIndex = 0;
            this.ValueText.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.ValueText.Enter += new System.EventHandler(this.ValueText_Enter);
            this.ValueText.KeyUp += new System.Windows.Forms.KeyEventHandler(this.ValueText_KeyUp);
            this.ValueText.Leave += new System.EventHandler(this.ValueText_Leave);
            // 
            // ValueControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Name = "ValueControl";
            this.SplitCont.Panel1.ResumeLayout(false);
            this.SplitCont.Panel2.ResumeLayout(false);
            this.SplitCont.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SplitCont)).EndInit();
            this.SplitCont.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.TextBox ValueText;
    }
}
