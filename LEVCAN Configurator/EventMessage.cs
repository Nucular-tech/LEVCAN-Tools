using ImGuiNET;
using LEVCAN.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace LEVCAN_Configurator
{
    public class EventMessage
    {
        LC_Event_t eventData;
        public ushort Sender { get => eventData.Sender; }
        public bool ToDelete = false;
        public EventMessage(LC_Event_t data)
        {
            eventData = data;
        }

        bool bringToFront = false;
        public void DrawMessage()
        {
            var size = ImGui.CalcTextSize(eventData.Text);
            var capsize = ImGui.CalcTextSize(eventData.Caption);
            if (size.X < capsize.X)
                size.X = capsize.X;

            ImGui.SetNextWindowSize(new Vector2(50, 75) + size);
            //override ID to CAN device ID, more stable result
            ImGui.Begin(eventData.Caption + $"###IDevent{Sender}", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse);
            if (bringToFront)
                ImGui.SetWindowFocus();

            ImGui.TextWrapped(eventData.Text);
            if (ImGui.Button("Close"))
            {
                ToDelete = true;
            }
            ImGui.End();
        }

        public void UpdateMessage(LC_Event_t data)
        {
            eventData = data;
            bringToFront = true;
        }
    }
}
