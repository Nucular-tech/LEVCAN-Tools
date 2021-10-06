namespace LEVCANsharpTest.paramControls
{
    partial class LabelControl
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
            this.NameLabel = new System.Windows.Forms.Label();
            this.TextLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // NameLabel
            // 
            this.NameLabel.BackColor = System.Drawing.Color.Transparent;
            this.NameLabel.Dock = System.Windows.Forms.DockStyle.Left;
            this.NameLabel.Location = new System.Drawing.Point(0, 0);
            this.NameLabel.Name = "NameLabel";
            this.NameLabel.Size = new System.Drawing.Size(180, 26);
            this.NameLabel.TabIndex = 2;
            this.NameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // TextLabel
            // 
            this.TextLabel.BackColor = System.Drawing.Color.Transparent;
            this.TextLabel.Dock = System.Windows.Forms.DockStyle.Right;
            this.TextLabel.Location = new System.Drawing.Point(188, 0);
            this.TextLabel.Name = "TextLabel";
            this.TextLabel.Size = new System.Drawing.Size(112, 26);
            this.TextLabel.TabIndex = 3;
            this.TextLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // LabelControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLight;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this.TextLabel);
            this.Controls.Add(this.NameLabel);
            this.Margin = new System.Windows.Forms.Padding(1);
            this.MinimumSize = new System.Drawing.Size(100, 26);
            this.Name = "LabelControl";
            this.Size = new System.Drawing.Size(300, 26);
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.Label NameLabel;
        public System.Windows.Forms.Label TextLabel;
    }
}
