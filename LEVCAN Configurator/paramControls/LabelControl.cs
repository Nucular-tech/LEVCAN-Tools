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
    }
}
