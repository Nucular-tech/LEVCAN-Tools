namespace LEVCANsharpTest.paramControls
{
    partial class BoolControl
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
            this.BoolCheckBox = new System.Windows.Forms.CheckBox();
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
            this.SplitCont.Panel2.Controls.Add(this.BoolCheckBox);
            this.SplitCont.Size = new System.Drawing.Size(298, 24);
            this.SplitCont.SplitterDistance = 178;
            // 
            // NameLabel
            // 
            this.NameLabel.Size = new System.Drawing.Size(178, 24);
            // 
            // BoolCheckBox
            // 
            this.BoolCheckBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.BoolCheckBox.Location = new System.Drawing.Point(0, 0);
            this.BoolCheckBox.Name = "BoolCheckBox";
            this.BoolCheckBox.Padding = new System.Windows.Forms.Padding(5, 0, 0, 0);
            this.BoolCheckBox.Size = new System.Drawing.Size(118, 24);
            this.BoolCheckBox.TabIndex = 3;
            this.BoolCheckBox.Text = "Disabled";
            this.BoolCheckBox.UseVisualStyleBackColor = true;
            // 
            // BoolControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Name = "BoolControl";
            this.SplitCont.Panel1.ResumeLayout(false);
            this.SplitCont.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.SplitCont)).EndInit();
            this.SplitCont.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        public System.Windows.Forms.CheckBox BoolCheckBox;
    }
}
