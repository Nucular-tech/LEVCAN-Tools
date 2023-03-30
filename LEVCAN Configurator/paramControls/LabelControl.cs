using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LEVCAN;

namespace LEVCANsharpTest.paramControls
{
    public partial class LabelControl : EntryPresenter
    {
        public LabelControl(LCPC_Entry entry) : base(entry)
        {
            InitializeComponent();
            this.ApplyFocuses();
            this.SaveBackColour();
            //nothing special
            NameLabel.Text = entry.Name;
            TextLabel.Text = entry.TextData;
        }

        private void NameLabel_MouseClick(object sender, MouseEventArgs e)
        {
            if(e.Button== MouseButtons.Right)
            {
                Clipboard.SetText(NameLabel.Text);
                toolTip1.Show("Copied to clipboard", this, e.X, e.Y, 2000);
            }
        }

        private void TextLabel_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                Clipboard.SetText(TextLabel.Text);
                toolTip1.Show("Copied to clipboard", this, e.X, e.Y, 2000);
            }
        }
    }
}
