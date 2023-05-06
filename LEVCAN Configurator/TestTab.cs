using HidSharp.Reports;
using ImGuiNET;
using LEVCAN;
using LEVCAN_Configurator.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Vortice.Mathematics;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace LEVCAN_Configurator
{
    struct DataLog
    {
        public ushort From;
        public uint Data1;
        public uint Data2;
    }

    internal class TestTab : IMGUI_TabInterface
    {
        LevcanHandler Lev;
        ImGuiTableFlags flags = ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersV | ImGuiTableFlags.Resizable | ImGuiTableFlags.Reorderable;
        Vector2 outer_size;
        ImGuiListClipperPtr clipper;
        bool startedTest = false;
        List<DataLog> dataLogs = new List<DataLog>();
        Timer timer;
        int requestcntr = 0;
        Timer randRequestTimer;

        public TestTab()
        { }

        public void Initialize(LevcanHandler lchandler, Settings settings)
        {
            Lev = lchandler;
            outer_size = new Vector2(0.0f, ImGui.GetTextLineHeight() * 18);
            ImGuiListClipper nativecli = new ImGuiListClipper();
            clipper = new ImGuiListClipperPtr(nativecli.ToIntPtr());
            timer = new Timer(100);
            timer.Elapsed += timer_Elapsed;
            randRequestTimer = new Timer(10);
            randRequestTimer.Elapsed += randRequestTimer_Elapsed;

            Lev.AddNodeObject(new LC_ObjectFunction((ushort)LC_SystemMessage.Trace, dataReceived, LC_ObjectAttributes.Writable, -8));
        }

        private void randRequestTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Lev.Node.SendRequest((ushort)LC_Address.Broadcast, (ushort)LC_Objects_Std.LC_Obj_ThrottleV);
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Lev.Node.SendRequest((ushort)LC_Address.Broadcast, LC_SystemMessage.Trace);
            requestcntr++;
        }

        public bool Draw()
        {
            if (ImGui.BeginTabItem($"TestTab {requestcntr}###testab"))
            {
                bool starteddisabled = false;
                if (startedTest == true)
                {
                    starteddisabled = true;
                    ImGui.BeginDisabled();
                }

                if (ImGui.Button("Start request test"))
                {
                    startedTest = true;
                    requestcntr = 0;
                    timer.Start();
                    randRequestTimer.Start();
                }
                if (starteddisabled == true)
                    ImGui.EndDisabled();
                ImGui.SameLine();
                if (ImGui.Button("Erase log"))
                {
                    dataLogs.Clear();
                    requestcntr = 0;
                }

                ImGui.SameLine();
                ImGui.Text($"Requests: {requestcntr}");
                ImGui.BeginChild("scroll_data_table");
                if (ImGui.BeginTable("table_scrollx", 3, flags, outer_size))
                {
                    ImGui.TableSetupScrollFreeze(0, 1);
                    ImGui.TableSetupColumn("Index #", ImGuiTableColumnFlags.NoHide, 100); // Make the first column not hideable to match our use of TableSetupScrollFreeze()
                    ImGui.TableSetupColumn("From", ImGuiTableColumnFlags.None, 100);
                    ImGui.TableSetupColumn("Data", ImGuiTableColumnFlags.None, 200);
                    ImGui.TableHeadersRow();
                    clipper.Begin(dataLogs.Count);
                    while (clipper.Step())
                    {
                        for (int row = clipper.DisplayStart; row < clipper.DisplayEnd; row++)
                        {
                            ImGui.TableNextRow();
                            ImGui.TableSetColumnIndex(0);
                            ImGui.Text($"{row}");
                            ImGui.TableSetColumnIndex(1);
                            ImGui.Text(dataLogs[row].From.ToString());
                            ImGui.TableSetColumnIndex(2);
                            ImGui.Text($"{dataLogs[row].Data1} {dataLogs[row].Data2}");
                        }
                    }
                    clipper.End();
                    if (ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
                        ImGui.SetScrollHereY(1.0f);
                    ImGui.EndTable();

                }
                ImGui.EndChild();
                ImGui.EndTabItem();
                return true;
            }
            else
                return false;
        }

        void dataReceived(LC_Header header, object data)
        {
            var log = new DataLog();
            log.From = header.Source;
            byte[] varb = (byte[])data;
            if (varb.Length == 0)
                return;
            if (varb.Length >= 4)
                log.Data1 = BitConverter.ToUInt32(varb.AsSpan(0, 4));

            if (varb.Length >= 8)
                log.Data2 = BitConverter.ToUInt32(varb.AsSpan(4, 4));
            dataLogs.Add(log);

            if (dataLogs.Count > 131072 / 8)
            {
                timer.Stop();
                randRequestTimer.Stop();
                startedTest = false;
                return;
            }

            Lev.Node.SendRequest(header.Source, LC_SystemMessage.Trace);
            requestcntr++;
            timer.Stop();
            timer.Start();
        }

    }
}
