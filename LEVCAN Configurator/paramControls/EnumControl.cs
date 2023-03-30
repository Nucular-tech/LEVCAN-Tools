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
    public partial class EnumControl : BaseControl
    {
        LCP_Enum enumInfo;
        string[] enumArray;
        string[] enumVisibleArray;
        EventHandler changedEvent;
        public EnumControl(LCPC_Entry entry) : base(entry)
        {
            InitializeComponent();
            this.ApplyFocuses();
            this.SaveBackColour();

            changedEvent = new EventHandler(ValueBox_SelectedIndexChanged);

            enumInfo = (LCP_Enum)entry.Descriptor;
            enumArray = entry.TextData.Split('\n');
            this.NameLabel.Text = entry.Name;

            if (entry.Mode.HasFlag(LCP_Mode.ReadOnly))
            {
                this.ValueBox.Enabled = false;
            }
            try
            {
                uint length = enumInfo.Size;
                if (enumArray.Length < length)
                    length = (uint)enumArray.Length;
                enumVisibleArray = new string[length];
                Array.Copy(enumArray, (int)enumInfo.Min, enumVisibleArray, 0, length);
            }
            catch { }

            SuspendUpdate.Suspend(ValueBox);
            ValueBox.DataSource = enumVisibleArray;
            ValueUpdated();
            SuspendUpdate.Resume(ValueBox);
        }

        bool noUpdates = false;
        public override void ValueUpdated()
        {
            if (noUpdates)
                return;
            ValueBox.SelectedIndexChanged -= changedEvent;

            uint varb = (uint)_entry.Variable;
            int index = (int)varb;
            index -= (int)enumInfo.Min;
            if (index < 0)
                index = -1;
            else if (index >= (int)(enumInfo.Size))
                index = -1; //out of range

            ValueBox.SelectedIndex = index;

            string text;
            //have this text with index?
            if (enumArray.Length > varb)
            {
                text = enumArray[varb];
            }
            else
            {
                text = varb.ToString();
            }
            ValueBox.Text = text;

            ValueBox.SelectedIndexChanged += changedEvent;
        }

        private async void ValueBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ValueBox.SelectedIndex >= 0)
            {
                _entry.Variable = enumInfo.Min + (uint)ValueBox.SelectedIndex;
                await _entry.SendVariable();
                await _entry.UpdateVariable();
            }
        }

        private void ValueBox_DropDown(object sender, EventArgs e)
        {
            noUpdates = true;
        }

        private void ValueBox_DropDownClosed(object sender, EventArgs e)
        {
            noUpdates = false;
        }

        override public async Task UpdateLive()
        {
            if (noUpdates)
                return;

            await base.UpdateLive();
        }
    }
}
