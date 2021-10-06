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
    public partial class BoolControl : BaseControl
    {
        string[] boolArray = { "Disabled", "Enabled" };
        public BoolControl(LCPC_Entry entry) : base(entry)
        {
            InitializeComponent();
            this.ApplyFocuses();
            this.SaveBackColour();

            NameLabel.Text = entry.Name;
            BoolCheckBox.Enabled = !entry.Mode.HasFlag(LCP_Mode.ReadOnly);

            if (entry.TextData != null)
            {
                var sep = entry.TextData.Split('\n');
                if (sep.Length == 2)
                    boolArray = sep;
            }
            ValueUpdated();
        }

        public override void ValueUpdated()
        {
            //do not trigger changes here
            BoolCheckBox.CheckedChanged -= BoolCheckBox_CheckedChanged;
            BoolCheckBox.Checked = (uint)_entry.Variable > 0;
            BoolCheckBox.CheckedChanged += BoolCheckBox_CheckedChanged;
            RefreshText();
        }

        void RefreshText()
        {
            if (BoolCheckBox.Checked)
            {
                BoolCheckBox.Text = boolArray[1];
            }
            else
            {
                BoolCheckBox.Text = boolArray[0];
            }
        }

        private async void BoolCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            RefreshText();
            if (sender != null)
            {
                _entry.Variable = BoolCheckBox.Checked ? (uint)1 : (uint)0;
                await _entry.SendVariable();
                await _entry.UpdateVariable();
            }
        }


    }
}
