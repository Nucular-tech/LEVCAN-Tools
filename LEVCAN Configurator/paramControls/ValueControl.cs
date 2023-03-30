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
using System.Text.RegularExpressions;
using System.Globalization;

namespace LEVCANsharpTest.paramControls
{
    public partial class ValueControl : BaseControl
    {
        static Regex patternSimple = new Regex(@"%(?:l{0,2}|h{0,2}|[jzt]{0,1})[diouxXeEfFgGaAcpsn]");
        static Regex pattern = new Regex(@"%[+\-0-9]*\.*([0-9]*)([xXeEfFdDgG])");
        string newFormat;
        bool focused = false;
        bool integer = false;

        public ValueControl(LCPC_Entry entry) : base(entry)
        {
            InitializeComponent();
            this.ApplyFocuses();
            this.SaveBackColour();
            normCol = ValueText.BackColor;
            NameLabel.Text = entry.Name;

            if (_entry.Mode.HasFlag(LCP_Mode.ReadOnly))
            {
                ValueText.ReadOnly = true;
            }

            if (entry.TextData != null)
            {
                //prepare for regex
                string csharpformat = entry.TextData.Replace("%%", "%").Replace("{", "{{").Replace("}", "}}");
                //replace complex %.2f
                newFormat = pattern.Replace(csharpformat, m =>
                {
                    if (m.Groups.Count == 3)
                    {
                        return "{0:" + m.Groups[2].Value + m.Groups[1].Value + "}";

                    }
                    return "{0}";
                });
                newFormat = patternSimple.Replace(newFormat, m => "{0}");
            }

            ValueUpdated();
        }

        private void ValueText_TextChanged(object sender, EventArgs e)
        {

        }

        public override void ValueUpdated()
        {
            var variable = _entry.Variable;
            string value = variable.ToString();
            var desc = _entry.Descriptor;

            object partA = 0;
            switch (_entry.EType)
            {
                case LCP_EntryType.Int32:
                    partA = (int)variable;
                    integer = true;
                    break;
                case LCP_EntryType.Uint32:
                    partA = (uint)variable;
                    integer = true;
                    break;
                case LCP_EntryType.Int64:
                    partA = (long)variable;
                    integer = true;
                    break;
                case LCP_EntryType.Uint64:
                    partA = (ulong)variable;
                    integer = true;
                    break;

                case LCP_EntryType.Float:
                case LCP_EntryType.Double:
                    partA = variable;
                    integer = false;
                    break;
                case LCP_EntryType.Decimal32:
                    partA = ToDecimals((int)variable, ((LCP_Decimal32)desc).Decimals);
                    if (((LCP_Decimal32)desc).Decimals > 0)
                        integer = false;
                    else
                        integer = true;
                    break;
            }
            if (newFormat != null)
                value = String.Format(newFormat, partA);
            else
                value = partA.ToString();

            ValueText.TextChanged -= ValueText_TextChanged;
            ValueText.Text = value;
            ValueText.TextChanged += ValueText_TextChanged;
        }

        string ToDecimals(int value, uint decimals)
        {
            return ToDecimals((long)value, decimals);
        }

        string ToDecimals(uint value, uint decimals)
        {
            return ToDecimals((ulong)value, decimals);
        }

        string ToDecimals(long value, uint decimals)
        {
            string outs;
            //possible some losses
            if (decimals != 0)
            {
                var divider = (long)Math.Pow(10, decimals);
                long vard = value / divider;
                long fract = value % divider;
                //vard.00fract
                outs = vard.ToString() + "." + fract.ToString("D" + decimals.ToString());
            }
            else
            {
                outs = value.ToString();
            }
            return outs;
        }

        string ToDecimals(ulong value, uint decimals)
        {
            string outs;
            //possible some losses
            if (decimals != 0)
            {
                var divider = (ulong)Math.Pow(10, decimals);
                ulong vard = value / divider;
                ulong fract = value % divider;
                //vard.00fract
                outs = vard.ToString() + "." + fract.ToString("D" + decimals.ToString());
            }
            else
            {
                outs = value.ToString();
            }
            return outs;
        }


        private void ValueText_Enter(object sender, EventArgs e)
        {
            focused = true;

        }
        Color normCol;

        private void ValueText_Leave(object sender, EventArgs e)
        {

            var valueString = subNumberOnly(ValueText.Text, false);
            bool parsed = false;
            object outputvalue = null;

            if (ValueText.ReadOnly)
                return;

            if (integer)
            {
                var styles = NumberStyles.Integer | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent;

                switch (_entry.EType)
                {
                    case LCP_EntryType.Decimal32:
                    case LCP_EntryType.Int32:
                        {
                            int value;
                            parsed = Int32.TryParse(valueString, styles, null, out value);
                            outputvalue = value;
                        }
                        break;
                    case LCP_EntryType.Uint32:
                        {
                            uint value;
                            parsed = UInt32.TryParse(valueString, styles, null, out value);
                            outputvalue = value;
                        }
                        break;
                    case LCP_EntryType.Int64:
                        {
                            long value;
                            parsed = Int64.TryParse(valueString, styles, null, out value);
                            outputvalue = value;
                        }
                        break;
                    case LCP_EntryType.Uint64:
                        {
                            ulong value;
                            parsed = UInt64.TryParse(valueString, styles, null, out value);
                            outputvalue = value;
                        }
                        break;
                }
            }
            else
            {
                double reslt = 0;
                parsed = Double.TryParse(valueString, out reslt);
                if (parsed)
                    switch (_entry.EType)
                    {
                        case LCP_EntryType.Float:
                            outputvalue = (float)reslt;
                            break;
                        case LCP_EntryType.Double:
                            outputvalue = reslt;
                            break;
                        case LCP_EntryType.Decimal32:
                            outputvalue = (int)(reslt * Math.Pow(10, ((LCP_Decimal32)_entry.Descriptor).Decimals));
                            break;
                    }
            }

            if (parsed)
            {
                _entry.Variable = outputvalue;
                focused = false;
                if (normCol != null)
                    ValueText.BackColor = normCol;
                ValueChanged();
            }
            else
            {
                normCol = ValueText.BackColor;
                ValueText.BackColor = Color.MediumVioletRed;
            }
        }

        private async void ValueChanged()
        {
            await _entry.SendVariable();
            await _entry.UpdateVariable();
            ValueUpdated();
        }

        private void ValueText_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                this.ActiveControl = null; //unfocus
            }
        }

        override public async Task UpdateLive()
        {
            if (focused)
                return;

            await base.UpdateLive();
        }

        string subNumberOnly(string input, bool hex)
        {
            string numericString = string.Empty;
            foreach (var c in input)
            {
                // Check for numeric characters (hex in this case) or leading or trailing spaces.
                if ((c >= '0' && c <= '9') || (char.ToUpperInvariant(c) >= 'A' && char.ToUpperInvariant(c) <= 'F' && hex) || c == ' ' || c == '.')
                {
                    numericString = string.Concat(numericString, c.ToString());
                }
                else
                {
                    break;
                }

            }
            return numericString;
        }
    }
}
