using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using LEVCAN;

namespace LEVCANsharpTest.paramControls
{
    public partial class BaseControl : EntryPresenter
    {
        private Color lineColor;
        private Pen linePen;

        public BaseControl(LCPC_Entry entry) : base(entry)
        {
            initThis();
        }

        public BaseControl() : base()
        {
            initThis();
        }

        void initThis()
        {
            InitializeComponent();

            this.LineColor = Color.DarkGray;
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            textBox1_TextChanged(null, null);

            this.ApplyFocuses();
            this.SaveBackColour();
        }

        public Color LineColor
        {
            get
            {
                return this.lineColor;
            }
            set
            {
                this.lineColor = value;

                this.linePen = new Pen(this.lineColor, 2);
                this.linePen.Alignment = PenAlignment.Inset;

                Refresh();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {

            var g = e.Graphics;
            int x = SplitCont.SplitterDistance + 1;

            g.DrawLine(linePen, x, 0, x, this.Height);
            base.OnPaint(e);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            Size size = TextRenderer.MeasureText(NameLabel.Text, NameLabel.Font, new Size(SplitCont.SplitterDistance, Int32.MaxValue), TextFormatFlags.WordBreak);

            if (size.Height > 26 - 2)
                this.Height = size.Height + 2;
            else
                this.Height = 26;
        }

        private void splitContainer1_SplitterMoved(object sender, SplitterEventArgs e)
        {
            textBox1_TextChanged(sender, e);
        }
        //assign this to the SplitContainer's MouseDown event
        private void splitCont_MouseDown(object sender, MouseEventArgs e)
        {
            // This disables the normal move behavior
            ((SplitContainer)sender).IsSplitterFixed = true;
        }

        //assign this to the SplitContainer's MouseUp event
        private void splitCont_MouseUp(object sender, MouseEventArgs e)
        {
            // This allows the splitter to be moved normally again
            ((SplitContainer)sender).IsSplitterFixed = false;
        }

        //assign this to the SplitContainer's MouseMove event
        private void splitCont_MouseMove(object sender, MouseEventArgs e)
        {
            // Check to make sure the splitter won't be updated by the
            // normal move behavior also
            if (((SplitContainer)sender).IsSplitterFixed)
            {
                // Make sure that the button used to move the splitter
                // is the left mouse button
                if (e.Button.Equals(MouseButtons.Left))
                {
                    // Checks to see if the splitter is aligned Vertically
                    if (((SplitContainer)sender).Orientation.Equals(Orientation.Vertical))
                    {
                        // Only move the splitter if the mouse is within
                        // the appropriate bounds
                        if (e.X > 0 && e.X < ((SplitContainer)sender).Width)
                        {
                            // Move the splitter & force a visual refresh
                            ((SplitContainer)sender).SplitterDistance = e.X;
                            ((SplitContainer)sender).Refresh();
                        }
                    }
                    // If it isn't aligned vertically then it must be
                    // horizontal
                    else
                    {
                        // Only move the splitter if the mouse is within
                        // the appropriate bounds
                        if (e.Y > 0 && e.Y < ((SplitContainer)sender).Height)
                        {
                            // Move the splitter & force a visual refresh
                            ((SplitContainer)sender).SplitterDistance = e.Y;
                            ((SplitContainer)sender).Refresh();
                        }
                    }
                }
                // If a button other than left is pressed or no button
                // at all
                else
                {
                    // This allows the splitter to be moved normally again
                    ((SplitContainer)sender).IsSplitterFixed = false;
                }
                textBox1_TextChanged(sender, e);
            }
        }
    }
}
