#nullable enable
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows.Forms;
using ImGuiNET;
using LEVCAN_Configurator.Properties;
using LEVCAN_Configurator_Shared;

namespace LEVCAN_Configurator.Tabs
{
    internal class DebugProbeTab : IMGUI_TabInterface, IDisposable
    {
        private LevcanHandler? _lev;
        private DebugProbeServer? _server;
        private int _port = 8585;
        private string _portInput = "8585";
        private string? _statusMessage;
        private DateTime _statusMessageExpiry = DateTime.MinValue;

        public void Initialize(LevcanHandler lchandler, Settings settings)
        {
            _lev = lchandler;
            _server = new DebugProbeServer(_lev, _port);

            // Auto-start probe on default port
            try
            {
                _server.Start();
            }
            catch (Exception ex)
            {
                _statusMessage = $"Auto-start failed on port {_port}: {ex.Message}";
                _statusMessageExpiry = DateTime.UtcNow.AddSeconds(10);
            }
        }

        public bool Draw()
        {
            if (!ImGui.BeginTabItem("Debug Probe"))
                return false;

            bool isRunning = _server?.IsRunning ?? false;

            ImGui.Spacing();

            // Clean, theme-native status display (ASCII only, no neon colors)
            string helpUrl = $"http://127.0.0.1:{_port}/help";

            if (isRunning)
            {
                ImGui.Text("Server Status: Running");
                ImGui.SameLine();

                // Selectable text field + Copy button directly to /help
                ImGui.SetNextItemWidth(240);
                string displayUrl = helpUrl;
                ImGui.InputText("##server_url", ref displayUrl, (uint)displayUrl.Length, ImGuiInputTextFlags.ReadOnly | ImGuiInputTextFlags.AutoSelectAll);

                ImGui.SameLine();
                if (ImGui.Button("Copy Help URL"))
                {
                    try
                    {
                        ImGui.SetClipboardText(helpUrl);
                    }
                    catch
                    {
                        try { Clipboard.SetText(helpUrl); } catch { }
                    }
                    ShowToast("Copied Help URL to clipboard!");
                }
            }
            else
            {
                ImGui.Text("Server Status: Stopped");
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // Controls: Start / Stop button and Port
            if (isRunning)
            {
                if (ImGui.Button("Stop Server", new Vector2(130, 28)))
                {
                    _server?.Stop();
                    ShowToast("Server stopped");
                }
            }
            else
            {
                if (ImGui.Button("Start Server", new Vector2(130, 28)))
                {
                    try
                    {
                        _server?.Start(_port);
                        ShowToast($"Server started on port {_port}");
                    }
                    catch (Exception ex)
                    {
                        ShowToast($"Failed to start: {ex.Message}");
                    }
                }

                ImGui.SameLine();
                ImGui.SetNextItemWidth(70);
                if (ImGui.InputText("Port", ref _portInput, 6))
                {
                    if (int.TryParse(_portInput, out var p) && p > 0 && p <= 65535)
                        _port = p;
                }
            }

            if (!string.IsNullOrEmpty(_statusMessage) && DateTime.UtcNow < _statusMessageExpiry)
            {
                ImGui.Spacing();
                ImGui.TextDisabled(_statusMessage);
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // Compact Recent Activity Log
            ImGui.TextDisabled("Recent Activity:");
            var logs = _server?.GetRecentLogs() ?? new List<ProbeRequestLog>();

            if (logs.Count == 0)
            {
                ImGui.TextDisabled("  No incoming requests yet.");
            }
            else
            {
                if (ImGui.BeginTable("compact_logs_table", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new Vector2(0, 160)))
                {
                    ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 70);
                    ImGui.TableSetupColumn("Method", ImGuiTableColumnFlags.WidthFixed, 60);
                    ImGui.TableSetupColumn("Path", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthFixed, 60);
                    ImGui.TableHeadersRow();

                    for (int i = logs.Count - 1; i >= 0 && i >= logs.Count - 15; i--)
                    {
                        var log = logs[i];
                        ImGui.TableNextRow();

                        ImGui.TableNextColumn();
                        ImGui.Text(log.Timestamp.ToString("HH:mm:ss"));

                        ImGui.TableNextColumn();
                        ImGui.Text(log.Method);

                        ImGui.TableNextColumn();
                        ImGui.Text(log.Path);

                        ImGui.TableNextColumn();
                        ImGui.Text(log.StatusCode.ToString());
                    }

                    ImGui.EndTable();
                }
            }

            ImGui.EndTabItem();
            return true;
        }

        private void ShowToast(string message)
        {
            _statusMessage = message;
            _statusMessageExpiry = DateTime.UtcNow.AddSeconds(5);
        }

        public void Dispose()
        {
            _server?.Dispose();
            _server = null;
        }
    }
}
