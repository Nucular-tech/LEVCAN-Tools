using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LEVCAN;

namespace LEVCANsharpTest.paramControls
{
    public partial class EntryPresenter : UserControl
    {
        internal LCPC_Entry _entry;
        public event EventHandler<ushort> FolderChange;

        public Color ClickedColour = SystemColors.ControlDark;
        public Color FocusedColour = SystemColors.Control;
        public Color UnfocusedColour = SystemColors.ControlLight;

        public EntryPresenter(LCPC_Entry entry)
        {
            _entry = entry;
        }

        public EntryPresenter()
        {

        }

        public void ApplyFocuses()
        {
            foreach (object objC in this.Controls)
            {
                //splitter kinda useless by itself, keep "splitter" with its own actions
                //but panel content is what really needed here
                if ((objC as SplitContainer) != null)
                {
                    var spl = objC as SplitContainer;

                    LookForControls(spl.Panel1.Controls);
                    LookForControls(spl.Panel2.Controls);

                    spl.Panel2.MouseEnter += Focusable_MouseEnter;
                    spl.Panel2.MouseLeave += Focusable_MouseLeave;
                    spl.Panel2.MouseDown += Focusable_MouseDown;
                    spl.Panel2.MouseUp += Focusable_MouseUp;

                    spl.Panel1.MouseEnter += Focusable_MouseEnter;
                    spl.Panel1.MouseLeave += Focusable_MouseLeave;
                    spl.Panel1.MouseDown += Focusable_MouseDown;
                    spl.Panel1.MouseUp += Focusable_MouseUp;
                }
                else
                {
                    var cntl = objC as Control;
                    cntl.MouseEnter += Focusable_MouseEnter;
                    cntl.MouseLeave += Focusable_MouseLeave;
                    cntl.MouseDown += Focusable_MouseDown;
                    cntl.MouseUp += Focusable_MouseUp;
                }
            }
            MouseEnter += Focusable_MouseEnter;
            MouseLeave += Focusable_MouseLeave;
            MouseDown += Focusable_MouseDown;
            MouseUp += Focusable_MouseUp;
        }

        public void SaveBackColour()
        {
            UnfocusedColour = this.BackColor;
        }

        void LookForControls(ControlCollection colll)
        {
            foreach (Control cntl in colll)
            {
                cntl.MouseEnter += Focusable_MouseEnter;
                cntl.MouseLeave += Focusable_MouseLeave;
                cntl.MouseDown += Focusable_MouseDown;
                cntl.MouseUp += Focusable_MouseUp;
            }
        }

        void OnEnter(EventArgs e)
        {
            //this.Select();
            this.BackColor = FocusedColour;
            base.OnEnter(e); // this will raise the Enter event
        }

        void OnLeave(EventArgs e)
        {
            this.BackColor = UnfocusedColour;
            base.OnLeave(e); // this will raise the Leave event
        }

        void OnClick(EventArgs e)
        {
            this.BackColor = ClickedColour;
            base.OnLeave(e); // this will raise the Leave event

            if (_entry.EType == LCP_EntryType.Folder)
            {
                FolderChange?.Invoke(this, _entry.FolderDirIndex);
            }
        }

        private void Focusable_MouseEnter(object sender, EventArgs e)
        {
            OnEnter(e);
        }

        private void Focusable_MouseLeave(object sender, EventArgs e)
        {
            OnLeave(e);
        }

        private void Focusable_MouseDown(object sender, MouseEventArgs e)
        {
            OnClick(e);
        }

        private void Focusable_MouseUp(object sender, MouseEventArgs e)
        {
            OnEnter(e);
        }

        public virtual void ValueUpdated()
        {

        }
        
        public virtual async Task ValueUserChanged()
        {
            await _entry.SendVariable();
            await _entry.UpdateVariable();
        }

        public virtual async Task UpdateLive()
        {
            if(_entry.Mode.HasFlag(LCP_Mode.LiveUpdate))
            {
                await _entry.UpdateVariable();
                ValueUpdated();
            }
        }
    }

    public static class SuspendUpdate
    {
        private const int WM_SETREDRAW = 0x000B;

        public static void Suspend(Control control)
        {
            Message msgSuspendUpdate = Message.Create(control.Handle, WM_SETREDRAW, IntPtr.Zero,
                IntPtr.Zero);

            NativeWindow window = NativeWindow.FromHandle(control.Handle);
            window.DefWndProc(ref msgSuspendUpdate);
        }

        public static void Resume(Control control)
        {
            // Create a C "true" boolean as an IntPtr
            IntPtr wparam = new IntPtr(1);
            Message msgResumeUpdate = Message.Create(control.Handle, WM_SETREDRAW, wparam,
                IntPtr.Zero);

            NativeWindow window = NativeWindow.FromHandle(control.Handle);
            window.DefWndProc(ref msgResumeUpdate);

            control.Invalidate();
        }
    }
}
