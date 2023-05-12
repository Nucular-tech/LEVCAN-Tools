using ImGuiNET;
using LEVCAN_Configurator.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LEVCAN_Configurator_Shared;

namespace LEVCAN_Configurator
{
    internal class SettingsTab : IMGUI_TabInterface
    {
        //Check CANDevice
        string[] devices = { "Nucular USB2CAN", "PCAN USB" };
        Vector2 left_settings_pane_vec = new Vector2(400, 0);
        FolderBrowserDialog fsfolder = new FolderBrowserDialog();
        bool cliceddebug = false;
        float itemssize;
        //settings stored/loaded
        int connectionIndex;
        string connectionPort;
        string comPrinterPort;
        string fileserver_path;
        string printerText;
        LevcanHandler Lev;
        Settings settings;
        public IntPtr logo = IntPtr.Zero;

        public void Initialize(LevcanHandler lchandler, Settings settings)
        {
            Lev = lchandler;
            this.settings = settings;
            InitSettings();
        }

        public bool Draw()
        {
            if (ImGui.BeginTabItem("Settings"))
            {
                ImGui.BeginChild("left pane", left_settings_pane_vec, false);
                ImGui.PushItemWidth(itemssize);
                
                if (ImGui.Combo("Connection select", ref connectionIndex, devices, devices.Length))
                {
                    Lev.DeviceSelect((CANDevice)connectionIndex);
                    settings.Connection = connectionIndex;
                }

                if (ImGui.InputText("Default port", ref connectionPort, 15))
                {
                    Lev.icanPort.SetDefaultPort(connectionPort);
                    settings.Port = connectionPort;
                }

                if (ImGui.InputText("COM Printer", ref comPrinterPort, 15))
                {
                    settings.COMPrinter = comPrinterPort;
                }
                ImGui.Text("Print text where %%s = value:");
                if (ImGui.InputTextMultiline("##textPrnt", ref printerText, 100, new Vector2(itemssize, 100)))
                {
                    settings.PrinterText = printerText;
                }

                ImGui.AlignTextToFramePadding();
                ImGui.Text("File server path:");
                ImGui.SameLine();
                ImGui.InputText("##fspath", ref fileserver_path, 250);

                if (ImGui.Button("Select server folder"))
                {
                    fsfolder.SelectedPath = Environment.CurrentDirectory + "/";
                    DialogResult result = DialogResult.Cancel;

                    result = fsfolder.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        try
                        {
                            string selected_new = fsfolder.SelectedPath;
                            if (selected_new.StartsWith(Environment.CurrentDirectory))
                                selected_new = Path.GetRelativePath(Environment.CurrentDirectory, selected_new);

                            fileserver_path = selected_new;
                            Lev.FileServer.SavePath = fileserver_path;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Try different path", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button("Open folder"))
                {
                    var psi = new System.Diagnostics.ProcessStartInfo() { FileName = "file://" + Path.GetFullPath(fileserver_path), UseShellExecute = true };
                    System.Diagnostics.Process.Start(psi);
                }
                ImGui.ShowStyleSelector("Styles");
                ImGui.PopItemWidth();
                if (ImGui.Button("Debug GUI"))
                {
                    cliceddebug = !cliceddebug;
                }
                if (cliceddebug)
                    ImGui.ShowDemoWindow();
                if (logo != IntPtr.Zero)
                {
                    ImGui.Text("Designed by:");
                    if (ImGui.ImageButton("Website", logo, new Vector2(300, 87)))
                    {
                        var psi = new System.Diagnostics.ProcessStartInfo() { FileName = "https://nucular.tech" , UseShellExecute = true };
                        System.Diagnostics.Process.Start(psi);
                    }
                }
                ImGui.EndChild();
                ImGui.EndTabItem();
                return true;
            }
            else
                return false;
        }

        public void InitSettings()
        {
            itemssize = ImGui.CalcTextSize(devices[0]).X + 50;
            connectionIndex = settings.Connection;
            connectionPort = settings.Port;
            comPrinterPort = settings.COMPrinter;
            fileserver_path = settings.FSpath;
            printerText = settings.PrinterText;
        }

    }
}
