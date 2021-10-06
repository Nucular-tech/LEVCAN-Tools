namespace LEVCANsharpTest.paramControls
{
    partial class EnumControl
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
            this.ValueBox = new System.Windows.Forms.ComboBox();
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
            this.SplitCont.Panel2.Controls.Add(this.ValueBox);
            this.SplitCont.Size = new System.Drawing.Size(298, 24);
            this.SplitCont.SplitterDistance = 178;
            // 
            // NameLabel
            // 
            this.NameLabel.Size = new System.Drawing.Size(178, 24);
            // 
            // ValueBox
            // 
            this.ValueBox.FormattingEnabled = true;
            this.ValueBox.Location = new System.Drawing.Point(2, 2);
            this.ValueBox.Name = "ValueBox";
            this.ValueBox.Size = new System.Drawing.Size(111, 21);
            this.ValueBox.TabIndex = 0;
            this.ValueBox.DropDown += new System.EventHandler(this.ValueBox_DropDown);
            this.ValueBox.DropDownClosed += new System.EventHandler(this.ValueBox_DropDownClosed);
            // 
            // EnumControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Name = "EnumControl";
            this.SplitCont.Panel1.ResumeLayout(false);
            this.SplitCont.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.SplitCont)).EndInit();
            this.SplitCont.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.ComboBox ValueBox;
    }
}
