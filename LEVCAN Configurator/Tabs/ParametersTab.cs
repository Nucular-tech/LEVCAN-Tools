using ImGuiNET;
using LEVCAN;
using LEVCAN_Configurator.Properties;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows.Forms;
using LEVCAN_Configurator_Shared;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace LEVCAN_Configurator
{
    internal class ParametersTab : IMGUI_TabInterface
    {
        Vector2 left_pane_vec = new Vector2(250, 0);
        Vector2 right_pane_vec = new Vector2(0, 0);
        Vector4 disabled_colour = new Vector4(100, 132, 155, 128);
        int request_name_timeout = 0;
        LCRemoteNode selected;
        LCRemoteNode new_selected;
        List<LCPC_Directory> pathSelect = new List<LCPC_Directory>();
        int updateNumber = 0, updateFrame = 0;
        LC_ParamClient paramClient;
        LevcanHandler Lev;
        Settings settings;

        public ParametersTab()
        {
        }

        public void Initialize(LevcanHandler lchandler, Settings settings)
        {
            Lev = lchandler;
            this.settings = settings;
        }

        public bool Draw()
        {
            if (ImGui.BeginTabItem("Parameters"))
            {
                if (request_name_timeout > 0)
                    request_name_timeout--;
                ImGui.BeginChild("left pane nodes", left_pane_vec, true);
                {
                    //remote LEVCAN device list
                    for (int ri = 0; ri < Lev.listOfRemotes.Count; ri++)
                    {
                        var remoteid = Lev.listOfRemotes[ri];
                        //fix for not ready local node
                        if (remoteid.Name == null && request_name_timeout == 0)
                        {
                            Lev.Node.SendRequest(remoteid.ShortName.NodeID, LC_SystemMessage.NodeName);
                            request_name_timeout = 10; //fps
                        }
                        //select item
                        if (ImGui.Selectable(remoteid.ToString(), selected == remoteid))
                        {
                            if (remoteid.ShortName.Configurable)
                            {
                                new_selected = remoteid;
                            }
                        }
                        ShowNodeContextMenu(remoteid);
                    }

                    ImGui.EndChild();
                }
                ImGui.SameLine();
                ImGui.BeginChild("right pane params", right_pane_vec, true);
                {
                    if (new_selected != null)
                    {
                        selected = new_selected;
                        new_selected = null;

                        paramClient = Lev.GetParametersClient(selected);
                        _ = paramClient.UpdateDirectoriesAsync();
                    }

                    if (paramClient != null && paramClient.ToBeDisposed)
                        paramClient = null;

                    if (paramClient != null && paramClient.Directories.Count > 0)
                    {
                        ImGui.PushItemWidth(200);

                        updateNumber = 0;

                        DrawParameters(paramClient.Directories, 0);
                        //update one parameter per frame, but not more than 4 times per sec
                        updateFrame++;
                        if (updateNumber < 15)
                            updateNumber = 15; // limit to 4hz
                        if (updateFrame > updateNumber)
                            updateFrame = 0;
                        ImGui.PopItemWidth();
                    }

                    ImGui.EndChild();
                }
                ImGui.EndTabItem();
                return true;
            }
            else
                return false;
        }

        unsafe void DrawParameters(List<LCPC_Directory> directories, int dindex)
        {
            if (dindex >= directories.Count)
                return;
            if (directories[dindex].Entries == null)
                return;

            for (int entryindex = 0; entryindex < directories[dindex].Entries.Count; entryindex++)
            {
                var entry = directories[dindex].Entries[entryindex];
                var flags = ImGuiInputTextFlags.EnterReturnsTrue;
                bool readonly_value = false;
                if (entry.Mode.HasFlag(LCP_Mode.ReadOnly))
                {
                    flags |= ImGuiInputTextFlags.ReadOnly;
                    readonly_value = true;
                    if (entry.EType != LCP_EntryType.Folder)
                        ImGui.PushStyleColor(ImGuiCol.FrameBg, 0x1FFFFFFF);
                }
                //safe for same parameter name
                ImGui.PushID(entryindex);
                switch (entry.EType)
                {
                    case LCP_EntryType.Folder:
                        if (ImGui.TreeNode(entry.Name))
                        { //recursive
                            DrawParameters(directories, entry.FolderDirIndex);
                            ImGui.TreePop();
                        }
                        break;

                    case LCP_EntryType.Label:
                        {
                            float x_offs = ImGui.GetCursorPosX();

                            ImGui.Text(entry.Name);
                            ShowContextMenuForText(entry.Name);

                            if (entry.TextData != null)
                            {
                                //offset
                                float x = ImGui.GetCursorPosX();
                                ImGui.SameLine();
                                if (x < ImGui.CalcItemWidth() + x_offs)
                                    ImGui.SetCursorPosX(ImGui.CalcItemWidth() + x_offs);

                                ImGui.PushID(entryindex);
                                ImGui.Text(entry.TextData);
                                ShowContextMenuForText(entry.TextData);
                                ImGui.PopID();
                            }
                        }
                        break;

                    case LCP_EntryType.Bool:
                        {
                            if (readonly_value) //this type does not have readonly
                                ImGui.BeginDisabled();

                            bool test = (bool)entry.Variable;
                            if (entry.TextData == null || entry.TextDataAsArray.Length < 2)
                            { //normal bool
                                if (ImGui.Checkbox(entry.Name, ref test))
                                {
                                    entry.Variable = test;
                                    _ = entry.SendVariable();
                                }
                                ShowContextMenu(entry.Name + " = " + entry.Variable.ToString());
                            }
                            else
                            { //text bool
                                int item = test ? 1 : 0;
                                if (ImGui.Combo(entry.Name, ref item, entry.TextDataAsArray, 2))
                                {
                                    entry.Variable = item == 1;
                                    _ = entry.SendVariable();
                                }
                                ShowContextMenu(entry.Name + " = " + entry.TextDataAsArray[item]);
                            }

                            if (readonly_value)
                                ImGui.EndDisabled();
                        }
                        break;

                    case LCP_EntryType.Enum:
                        {
                            uint item = Convert.ToUInt32(entry.Variable);
                            var desc = ((LCP_Enum)entry.Descriptor);
                            ImGuiComboFlags fl = 0;
                            if (readonly_value)
                            {
                                fl = ImGuiComboFlags.NoArrowButton;
                            }
                            //custom combo box with only visible parameters
                            string preview;
                            if (item >= entry.TextDataAsArray.Length)
                                preview = item.ToString();
                            else
                                preview = entry.TextDataAsArray[item];
                            if (ImGui.BeginCombo(entry.Name, preview, fl))
                            {
                                //show visible range
                                for (var ci = 0; ci < desc.Size && ci < entry.TextDataAsArray.Length; ci++)
                                {
                                    bool selected = (item - desc.Min) == ci;
                                    if (ImGui.Selectable(entry.TextDataAsArray[ci], selected))
                                    {
                                        item = (uint)(ci + desc.Min);
                                        entry.Variable = item;
                                        _ = entry.SendVariable();
                                    }

                                    // Set the initial focus when opening the combo (scrolling + keyboard navigation focus)
                                    if (selected)
                                        ImGui.SetItemDefaultFocus();
                                }
                                ImGui.EndCombo();
                            }
                            ShowContextMenu(entry.Name + " = " + preview);
                        }
                        break;

                    case LCP_EntryType.Int32:
                        {
                            int value = Convert.ToInt32(entry.Variable);
                            int step = ((LCP_Int32)entry.Descriptor).Step;
                            if (ImGui.InputScalar(entry.Name, ImGuiDataType.S32, (IntPtr)(&value), readonly_value ? IntPtr.Zero : (IntPtr)(&step), (IntPtr)(&step), entry.TextData, flags))
                            {
                                entry.Variable = value;
                                _ = entry.SendVariable();
                            }

                            ShowContextMenuForText(entry.Name + " = " + value.ToString());
                        }
                        break;

                    case LCP_EntryType.Float:
                        {
                            float valf32 = (float)entry.Variable;
                            if (ImGui.InputFloat(entry.Name, ref valf32, ((LCP_Float)entry.Descriptor).Step, ((LCP_Float)entry.Descriptor).Step * 2, entry.TextData, flags))
                            {
                                entry.Variable = valf32;
                                _ = entry.SendVariable();
                            }
                            ShowContextMenuForText(entry.Name + " = " + valf32.ToString());
                        }
                        break;

                    case LCP_EntryType.Uint32:
                        {
                            uint value = Convert.ToUInt32(entry.Variable);
                            uint step = ((LCP_Uint32)entry.Descriptor).Step;
                            if (ImGui.InputScalar(entry.Name, ImGuiDataType.U32, (IntPtr)(&value), readonly_value ? IntPtr.Zero : (IntPtr)(&step), IntPtr.Zero, entry.TextData, flags))
                            {
                                entry.Variable = value;
                                _ = entry.SendVariable();
                            }
                            ShowContextMenuForText(entry.Name + " = " + value.ToString());
                        }
                        break;

                    case LCP_EntryType.Int64:
                        {
                            long value = (long)entry.Variable;
                            long step = ((LCP_Int64)entry.Descriptor).Step;
                            if (ImGui.InputScalar(entry.Name, ImGuiDataType.S64, (IntPtr)(&value), readonly_value ? IntPtr.Zero : (IntPtr)(&step), IntPtr.Zero, entry.TextData, flags))
                            {
                                entry.Variable = value;
                                _ = entry.SendVariable();
                            }
                            ShowContextMenuForText(entry.Name + " = " + value.ToString());
                        }
                        break;

                    case LCP_EntryType.Uint64:
                        {
                            ulong value = (ulong)entry.Variable;
                            ulong step = ((LCP_Uint64)entry.Descriptor).Step;
                            if (ImGui.InputScalar(entry.Name, ImGuiDataType.U64, (IntPtr)(&value), readonly_value ? IntPtr.Zero : (IntPtr)(&step), IntPtr.Zero, entry.TextData, flags))
                            {
                                entry.Variable = value;
                                _ = entry.SendVariable();
                            }
                            ShowContextMenuForText(entry.Name + " = " + value.ToString());
                        }
                        break;

                    case LCP_EntryType.Double:
                        {
                            double value = (double)entry.Variable;
                            double step = ((LCP_Double)entry.Descriptor).Step;
                            if (ImGui.InputScalar(entry.Name, ImGuiDataType.Double, (IntPtr)(&value), readonly_value ? IntPtr.Zero : (IntPtr)(&step), IntPtr.Zero, entry.TextData, flags))
                            {
                                entry.Variable = value;
                                _ = entry.SendVariable();
                            }
                            ShowContextMenuForText(entry.Name + " = " + value.ToString());
                        }
                        break;

                    case LCP_EntryType.Decimal32:
                        {
                            int value = Convert.ToInt32(entry.Variable);
                            var descr = (LCP_Decimal32)entry.Descriptor;
                            string decitext = ToDecimals(value, descr);
                            //check for formatting
                            if (entry.TextData != null && entry.TextData.Contains("%s"))
                            {
                                decitext = entry.TextData.Replace("%s", decitext);
                            }
                            if (decitext.Contains("%%"))
                            {
                                decitext = decitext.Replace("%%", "%");
                            }
                            bool value_changed = false;
                            float button_size = ImGui.GetFrameHeight();
                            var styleSpacingX = ImGui.GetStyle().ItemInnerSpacing.X;

                            int steps = 50;
                            if (descr.Step != 0)
                                steps = (descr.Max - descr.Min) / descr.Step;
                            if (steps < 50 && !readonly_value)
                            {
                                //for low step count use slider
                                int sliderIndex = (value - descr.Min + descr.Step / 2) / descr.Step;
                                if (ImGui.SliderInt(entry.Name, ref sliderIndex, 0, steps, decitext, ImGuiSliderFlags.NoInput))
                                {
                                    value = sliderIndex * descr.Step + descr.Min;
                                    value_changed = true;
                                }
                            }
                            else
                            {
                                //check if can draw +-
                                bool addPlusMinus = (descr.Step != 0 && !readonly_value);
                                if (addPlusMinus)
                                    ImGui.SetNextItemWidth(Math.Max(1.0f, ImGui.CalcItemWidth() - (button_size + styleSpacingX) * 2));

                                //normal text input
                                if (ImGui.InputText(addPlusMinus ? "##input" : entry.Name, ref decitext, 128, flags))
                                {
                                    value = FromDecimal(decitext, descr);
                                    value_changed = true;
                                }
                                if (addPlusMinus)
                                {
                                    // Draw buttons for our weird data type
                                    if (readonly_value)
                                        ImGui.BeginDisabled();

                                    ImGui.PushButtonRepeat(true);
                                    ImGui.SameLine(0, styleSpacingX);
                                    var btsize = new Vector2(button_size, button_size);
                                    if (ImGui.Button("-", btsize))
                                    {
                                        value -= descr.Step;
                                        value_changed = true;
                                    }
                                    ImGui.SameLine(0, styleSpacingX);
                                    if (ImGui.Button("+", btsize))
                                    {
                                        value += descr.Step;
                                        value_changed = true;
                                    }
                                    ImGui.PopButtonRepeat();

                                    if (readonly_value)
                                        ImGui.EndDisabled();
                                    //parameter name hidden in textinput
                                    ImGui.SameLine(0, styleSpacingX);
                                    ImGui.Text(entry.Name);
                                }
                            }
                            if (value_changed)
                            {
                                entry.Variable = value;
                                _ = entry.SendVariable();
                            }
                            ShowContextMenuForText(entry.Name + " = " + decitext);
                        }
                        break;

                    case LCP_EntryType.Bitfield32:
                        {
                            //TODO add editor
                            uint item = Convert.ToUInt32(entry.Variable);
                            ImGui.Text(entry.Name + " = 0b" + Convert.ToString(item, 2));
                            ShowContextMenuForText(entry.Name);
                        }
                        break;

                    case LCP_EntryType.String:
                        {
                            //TODO add editor
                            ImGui.Text((string)entry.Variable);
                            ShowContextMenuForText(entry.Name + " = " + (string)entry.Variable);
                        }
                        break;
                }
                ImGui.PopID();
                if (readonly_value && entry.EType != LCP_EntryType.Folder)
                    ImGui.PopStyleColor();
                //todo: change visible check to per-item to avoid overlap with context menu
                if (entry.Mode.HasFlag(LCP_Mode.LiveUpdate) && ImGui.IsItemVisible())
                {
                    //slow update
                    if (updateNumber == updateFrame)
                        _ = entry.UpdateVariable();
                    updateNumber++;
                }
            }
        }

        string ToDecimals(int value, LCP_Decimal32 descr)
        {
            return ToDecimals((long)value, descr.Decimals);
        }

        string ToDecimals(long value, uint decimals)
        {
            string outs;
            //possible some losses
            if (decimals != 0)
            {
                var divider = (long)Math.Pow(10, decimals);
                long vard = value / divider;
                long fract = Math.Abs(value % divider);
                //vard.00fract
                outs = vard.ToString() + "." + fract.ToString("D" + decimals.ToString());
            }
            else
            {
                outs = value.ToString();
            }
            return outs;
        }

        int FromDecimal(string text, LCP_Decimal32 descr)
        {
            double reslt = 0;
            string onlynumber = new string(text.Trim().TakeWhile(c => char.IsDigit(c) || c == '.' || c == '-').ToArray());
            bool parsed = Double.TryParse(onlynumber, out reslt);
            if (parsed)
                return (int)(reslt * Math.Pow(10, descr.Decimals));
            else
                return 0;
        }

        void ShowContextMenuForText(string text)
        {
            //fixed uid because every parameter is nested
            if (ImGui.BeginPopupContextItem("popup"))
            {
                ShowContextBody(text);
            }
        }

        void ShowContextMenu(string copytext)
        {
            if (ImGui.BeginPopupContextItem())
            {
                ShowContextBody(copytext);
            }
        }

        void ShowContextBody(string copytext)
        {
            if (ImGui.Selectable("Copy"))
                Clipboard.SetText(copytext);
            if (ImGui.Selectable("Copy and Print"))
            {
                Clipboard.SetText(copytext);
                PrinterCOM.Print(settings.PrinterText.Replace("%s", copytext), settings.COMPrinter);
            }
            ImGui.EndPopup();
        }

        void ShowNodeContextMenu(LCRemoteNode remoteid)
        {
            if (ImGui.BeginPopupContextItem())
            {
                if (ImGui.Selectable("Copy"))
                    Clipboard.SetText(remoteid.ToString());
                if (ImGui.Selectable("Force update"))
                    Lev.Node.SendData(new byte[] { 0xDA, 0xCE, 0xCA, 0x02 }, (byte)remoteid.ShortName.NodeID, (ushort)LC_SystemMessage.SWUpdate);
                ImGui.EndPopup();
            }
        }

    }
}
