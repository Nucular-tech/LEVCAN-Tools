namespace LEVCANsharpTest
{
    partial class MainWindow
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.listBox_devices = new System.Windows.Forms.ListBox();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.label1 = new System.Windows.Forms.Label();
            this.labelSettings = new System.Windows.Forms.Label();
            this.flowSettingsPanel = new LEVCANsharpTest.FlowNoScroll();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.errorsLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.txrxLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.label2 = new System.Windows.Forms.ToolStripStatusLabel();
            this.label5 = new System.Windows.Forms.ToolStripStatusLabel();
            this.label4 = new System.Windows.Forms.ToolStripStatusLabel();
            this.buttonBack = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.statusStrip1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // listBox_devices
            // 
            this.listBox_devices.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.listBox_devices.FormattingEnabled = true;
            this.listBox_devices.IntegralHeight = false;
            this.listBox_devices.Location = new System.Drawing.Point(0, 16);
            this.listBox_devices.Name = "listBox_devices";
            this.listBox_devices.Size = new System.Drawing.Size(187, 318);
            this.listBox_devices.TabIndex = 3;
            this.listBox_devices.SelectedValueChanged += new System.EventHandler(this.listBox1_SelectedValueChanged);
            // 
            // timer1
            // 
            this.timer1.Interval = 500;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(46, 13);
            this.label1.TabIndex = 7;
            this.label1.Text = "Devices";
            // 
            // labelSettings
            // 
            this.labelSettings.AutoSize = true;
            this.labelSettings.Location = new System.Drawing.Point(3, 0);
            this.labelSettings.Name = "labelSettings";
            this.labelSettings.Size = new System.Drawing.Size(45, 13);
            this.labelSettings.TabIndex = 7;
            this.labelSettings.Text = "Settings";
            // 
            // flowSettingsPanel
            // 
            this.flowSettingsPanel.AutoScroll = true;
            this.flowSettingsPanel.BackColor = System.Drawing.SystemColors.Window;
            this.flowSettingsPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.flowSettingsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowSettingsPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowSettingsPanel.Location = new System.Drawing.Point(3, 16);
            this.flowSettingsPanel.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this.flowSettingsPanel.Name = "flowSettingsPanel";
            this.flowSettingsPanel.Size = new System.Drawing.Size(408, 287);
            this.flowSettingsPanel.TabIndex = 11;
            this.flowSettingsPanel.WrapContents = false;
            // 
            // statusStrip1
            // 
            this.statusStrip1.AllowMerge = false;
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.errorsLabel,
            this.txrxLabel,
            this.label2,
            this.label5,
            this.label4});
            this.statusStrip1.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
            this.statusStrip1.Location = new System.Drawing.Point(0, 366);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(619, 22);
            this.statusStrip1.TabIndex = 12;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // errorsLabel
            // 
            this.errorsLabel.Name = "errorsLabel";
            this.errorsLabel.Size = new System.Drawing.Size(45, 17);
            this.errorsLabel.Text = "rxLabel";
            // 
            // txrxLabel
            // 
            this.txrxLabel.Name = "txrxLabel";
            this.txrxLabel.Size = new System.Drawing.Size(45, 17);
            this.txrxLabel.Text = "txLabel";
            // 
            // label2
            // 
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(35, 17);
            this.label2.Text = "delay";
            // 
            // label5
            // 
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(58, 17);
            this.label5.Text = "Maxdelay";
            // 
            // label4
            // 
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(28, 17);
            this.label4.Text = "cntr";
            // 
            // buttonBack
            // 
            this.buttonBack.Location = new System.Drawing.Point(3, 307);
            this.buttonBack.Name = "buttonBack";
            this.buttonBack.Size = new System.Drawing.Size(75, 23);
            this.buttonBack.TabIndex = 13;
            this.buttonBack.Text = "<< Back";
            this.buttonBack.UseVisualStyleBackColor = true;
            this.buttonBack.Click += new System.EventHandler(this.buttonBack_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(619, 366);
            this.tabControl1.TabIndex = 14;
            // 
            // tabPage1
            // 
            this.tabPage1.BackColor = System.Drawing.SystemColors.Control;
            this.tabPage1.Controls.Add(this.splitContainer1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(611, 340);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Devices and settings";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(3, 3);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.listBox_devices);
            this.splitContainer1.Panel1.Controls.Add(this.label1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.tableLayoutPanel1);
            this.splitContainer1.Size = new System.Drawing.Size(605, 334);
            this.splitContainer1.SplitterDistance = 187;
            this.splitContainer1.TabIndex = 14;
            this.splitContainer1.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.splitContainer1_SplitterMoved);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.labelSettings, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.flowSettingsPanel, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.buttonBack, 0, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 15F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(414, 334);
            this.tableLayoutPanel1.TabIndex = 14;
            // 
            // tabPage2
            // 
            this.tabPage2.BackColor = System.Drawing.SystemColors.Control;
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(611, 340);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "File server";
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(619, 388);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.statusStrip1);
            this.Name = "MainWindow";
            this.Text = "LEV-CAN Tool";
            this.ResizeEnd += new System.EventHandler(this.MainWindow_ResizeEnd);
            this.SizeChanged += new System.EventHandler(this.MainWindow_SizeChanged);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ListBox listBox_devices;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label labelSettings;
        private FlowNoScroll flowSettingsPanel;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel errorsLabel;
        private System.Windows.Forms.ToolStripStatusLabel txrxLabel;
        private System.Windows.Forms.ToolStripStatusLabel label2;
        private System.Windows.Forms.ToolStripStatusLabel label5;
        private System.Windows.Forms.ToolStripStatusLabel label4;
        private System.Windows.Forms.Button buttonBack;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
    }
}

