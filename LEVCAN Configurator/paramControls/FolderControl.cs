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
    public partial class FolderControl : EntryPresenter
    {
        public FolderControl(LCPC_Entry entry) : base(entry)
        {
            InitializeComponent();
            this.ApplyFocuses();
            this.SaveBackColour();
            
            NameLabel.Text = entry.Name;
        }
    }
}
