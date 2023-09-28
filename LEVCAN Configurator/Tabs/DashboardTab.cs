using ImGuiKnobs;
using ImGuiNET;
using LEVCAN;
using LEVCAN_Configurator.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using LEVCAN_Configurator_Shared;

namespace LEVCAN_Configurator.Tabs
{

    internal class DashboardTab : IMGUI_TabInterface
    {
        LevcanHandler Lev;
        Dictionary<ushort, DeviceInfo> deviceList = new Dictionary<ushort, DeviceInfo>();
        Knob barknob;
        ImFontPtr bigNumberFont;
        Vector2 sizeMin = new Vector2(250, 150);
        Vector2 sizeMax = new Vector2(600, 600);
        Vector2 positionOffset = new Vector2(5, 35);
        byte[] brakeData;
        byte[] noBrakeData;
        bool braked = false;
        LC_Obj_Buttons_t buttons = new LC_Obj_Buttons_t();

        public void Initialize(LevcanHandler lchandler, Settings settings)
        {
            Lev = lchandler;
            Lev.AddNodeObject(new LC_ObjectFunction((ushort)LC_Objects_Std.LC_Obj_DCSupply, ProcessMessage, LC_ObjectAttributes.Writable, typeof(LC_Obj_Supply_t)));
            Lev.AddNodeObject(new LC_ObjectFunction((ushort)LC_Objects_Std.LC_Obj_MotorSupply, ProcessMessage, LC_ObjectAttributes.Writable, typeof(LC_Obj_Supply_t)));
            Lev.AddNodeObject(new LC_ObjectFunction((ushort)LC_Objects_Std.LC_Obj_InternalVoltage, ProcessMessage, LC_ObjectAttributes.Writable, typeof(LC_Obj_InternalVoltage_t)));
            Lev.AddNodeObject(new LC_ObjectFunction((ushort)LC_Objects_Std.LC_Obj_Power, ProcessMessage, LC_ObjectAttributes.Writable, typeof(LC_Obj_Power_t)));
            Lev.AddNodeObject(new LC_ObjectFunction((ushort)LC_Objects_Std.LC_Obj_Temperature, ProcessMessage, LC_ObjectAttributes.Writable, typeof(LC_Obj_Temperature_t)));
            Lev.AddNodeObject(new LC_ObjectFunction((ushort)LC_Objects_Std.LC_Obj_RPM, ProcessMessage, LC_ObjectAttributes.Writable, typeof(LC_Obj_RPM_t)));
            Lev.AddNodeObject(new LC_ObjectFunction((ushort)LC_Objects_Std.LC_Obj_CellMinMax, ProcessMessage, LC_ObjectAttributes.Writable, typeof(LC_Obj_CellMinMax_t)));
            Lev.AddNodeObject(new LC_ObjectFunction((ushort)LC_Objects_Std.LC_Obj_CellsV, ProcessMessage, LC_ObjectAttributes.Writable, -194)); //up to 96 cells
            Lev.AddNodeObject(new LC_ObjectFunction((ushort)LC_Objects_Std.LC_Obj_CellBalance, ProcessMessage, LC_ObjectAttributes.Writable, -32));
            Lev.AddNodeObject(new LC_ObjectFunction((ushort)LC_Objects_Std.LC_Obj_ActiveFunctions, ProcessMessage, LC_ObjectAttributes.Writable, typeof(LC_Obj_ActiveFunctions_t)));
            barknob = new Knob(ImGuiDataType.Float, ImGuiKnobVariant.WiperOnly, 110, ImGuiKnobFlags.BottomTitle | ImGuiKnobFlags.NoInput | ImGuiKnobFlags.NoHover | ImGuiKnobFlags.CenterValue, 0.6f, 0.7f);

            if (ImGui.GetIO().Fonts.Fonts.Size > 1)
                bigNumberFont = ImGui.GetIO().Fonts.Fonts[1];
            else
                bigNumberFont = ImGui.GetIO().Fonts.Fonts[0];

            LC_Obj_ControlFactorInt_t brk = new LC_Obj_ControlFactorInt_t();
            brk.BrakeFactor = 10000;
            brakeData = CastingHelper.CastToArray(brk);
            brk.BrakeFactor = 100;
            noBrakeData = CastingHelper.CastToArray(brk);
        }

        private void ProcessMessage(LC_Header header, object data)
        {
            if (!deviceList.ContainsKey(header.Source))
                return;
            var deviceinfo = deviceList[header.Source];
            switch (header.MsgID)
            {
                case (ushort)LC_Objects_Std.LC_Obj_DCSupply:
                    deviceinfo.DCSupply = (LC_Obj_Supply_t)data; break;

                case (ushort)LC_Objects_Std.LC_Obj_MotorSupply:
                    deviceinfo.MotorSupply = (LC_Obj_Supply_t)data; break;

                case (ushort)LC_Objects_Std.LC_Obj_InternalVoltage:
                    deviceinfo.InterVoltage = (LC_Obj_InternalVoltage_t)data; break;

                case (ushort)LC_Objects_Std.LC_Obj_Power:
                    deviceinfo.Power = (LC_Obj_Power_t)data; break;

                case (ushort)LC_Objects_Std.LC_Obj_Temperature:
                    deviceinfo.Temp = (LC_Obj_Temperature_t)data; break;

                case (ushort)LC_Objects_Std.LC_Obj_RPM:
                    deviceinfo.RPMSpeed = (LC_Obj_RPM_t)data; break;

                case (ushort)LC_Objects_Std.LC_Obj_CellMinMax:
                    deviceinfo.CellMinMax = (LC_Obj_CellMinMax_t)data; break;

                case (ushort)LC_Objects_Std.LC_Obj_ActiveFunctions:
                    deviceinfo.ActiveFunc = (LC_Obj_ActiveFunctions_t)data; break;

                case (ushort)LC_Objects_Std.LC_Obj_CellsV:
                    byte[] databytes = (byte[])data;
                    int lenght16b = (int)Math.Ceiling((float)databytes.Length / 2);
                    if (lenght16b < 2)
                        break;
                    short[] sdata = new short[lenght16b - 1];
                    Buffer.BlockCopy(databytes, 2, sdata, 0, databytes.Length - 2);
                    deviceinfo.CellV = sdata;
                    break;

                default:
                    break;
            }
        }

        public bool Draw()
        {
            if (ImGui.BeginTabItem("Dashboard"))
            {
                ImGui.AlignTextToFramePadding(); ImGui.Text("Controls:"); ImGui.SameLine();
                ImGui.Checkbox($"Brake", ref braked);
                if (braked)
                    buttons.Buttons = LC_Obj_Buttons_Bt.Brake;
                else
                    buttons.Buttons &= ~LC_Obj_Buttons_Bt.Brake;

                for (int i = 1; i < 9; i++)
                {
                    bool checkvalue = (buttons.extraButtons & (1 << (i - 1))) != 0;
                    ImGui.SameLine();
                    ImGui.Checkbox($"CAN {i}", ref checkvalue);
                    if (checkvalue)
                        buttons.extraButtons |= (ushort)(1 << (i - 1));
                    else
                        buttons.extraButtons &= (ushort)~(1 << (i - 1));
                }
                Lev.Node.SendData(CastingHelper.CastToArray(buttons), (byte)LC_Address.Broadcast, (ushort)LC_Objects_Std.LC_Obj_Buttons);


                ImGui.BeginChild("deviceInfoChild");
                for (int ri = 0; ri < Lev.listOfRemotes.Count; ri++)
                {
                    var remoteid = Lev.listOfRemotes[ri];
                    if (!deviceList.ContainsKey(remoteid.ShortName.NodeID))
                    {
                        deviceList.Add(remoteid.ShortName.NodeID, new DeviceInfo());
                    }
                    var deviceInfo = deviceList[remoteid.ShortName.NodeID];

                    switch (remoteid.ShortName.DeviceType)
                    {
                        case (ushort)LC_Device.LC_Device_Controller:
                            DrawControllerWindow(remoteid, deviceInfo);
                            break;
                        case (ushort)LC_Device.LC_Device_BMS:
                            DrawBMSWindow(remoteid, deviceInfo);
                            break;
                        case (ushort)LC_Device.LC_Device_Debug:
                        case (ushort)LC_Device.LC_Device_Light:
                            DrawuLightWindow(remoteid, deviceInfo);
                            break;
                    }
                }
                ImGui.EndChild();
                ImGui.EndTabItem();
                return true;
            }
            return false;
        }

        float testv = 40;
        void DrawControllerWindow(LCRemoteNode remote, DeviceInfo info)
        {
            ImGui.SetNextWindowPos(positionOffset, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(sizeMin, sizeMax);
            ImGui.Begin(remote.ToString() + $"###IDwindow{remote.ShortName.NodeID}", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize);
            ClampBorder();
            //Controller area
            //Icons
            var speed = ((int)info.ActiveFunc.Functions & (int)LC_Obj_Buttons_Bt.Speed_Mask) >> (int)LC_Obj_Buttons_Bt.Speed_Offset;
            for (int i = 0; i < 4; i++)
            {
                if (speed != i)
                    ImGui.BeginDisabled();
                if (i == 0)
                    ImGui.SmallButton("N");
                else
                    ImGui.SmallButton($"S{i}");

                if (speed != i)
                    ImGui.EndDisabled();
                ImGui.SameLine();
            }
            bool hasflag = false; 
            if (!(hasflag = info.ActiveFunc.Functions.HasFlag(LC_Obj_ActiveFunctions_e.Brake)))
                ImGui.BeginDisabled();
            ImGui.SmallButton("BK");
            if (!hasflag)
                ImGui.EndDisabled();
            ImGui.SameLine();

            if (!(hasflag = info.ActiveFunc.Functions.HasFlag(LC_Obj_ActiveFunctions_e.EBrakeLimited)))
                ImGui.BeginDisabled();
            ImGui.SmallButton("!BK!");
            if (!hasflag)
                ImGui.EndDisabled();
            ImGui.SameLine();


            if (!(hasflag = info.ActiveFunc.Functions.HasFlag(LC_Obj_ActiveFunctions_e.Cruise)))
                ImGui.BeginDisabled();
            ImGui.SmallButton("CR");
            if (!hasflag)
                ImGui.EndDisabled();

            //NEXT LINE
            if (!(hasflag = info.ActiveFunc.Functions.HasFlag(LC_Obj_ActiveFunctions_e.ConverterMode)))
                ImGui.BeginDisabled();
            ImGui.SmallButton("DC-DC");
            if (!hasflag)
                ImGui.EndDisabled();
            ImGui.SameLine();

            if (!(hasflag = info.ActiveFunc.Functions.HasFlag(LC_Obj_ActiveFunctions_e.MotorFail)))
                ImGui.BeginDisabled();
            ImGui.SmallButton("MOTOR");
            if (!hasflag)
                ImGui.EndDisabled();
            ImGui.SameLine();

            if (!(hasflag = info.ActiveFunc.Functions.HasFlag(LC_Obj_ActiveFunctions_e.ControllerFail)))
                ImGui.BeginDisabled();
            ImGui.SmallButton("CONTR");
            if (!hasflag)
                ImGui.EndDisabled();
            ImGui.SameLine();

            if (!(hasflag = info.ActiveFunc.Functions.HasFlag(LC_Obj_ActiveFunctions_e.CruiseReady)))
                ImGui.BeginDisabled();
            ImGui.SmallButton("CR-RDY");
            if (!hasflag)
                ImGui.EndDisabled();

            ImGui.Separator();
            //Voltage and currents
            ImGui.PushFont(bigNumberFont);
            ImGui.Text($"{info.DCSupply.Voltage / 1000.0f:0.00}V");
            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetWindowWidth() / 2);
            ImGui.Text($"{info.WattageFromSupply:0.}W");
            ImGui.PopFont();

            barknob.DrawValueKnob("Controller", $"{info.InternalTemp}°C", info.InternalTemp / 100.0f);
            ImGui.SameLine();
            barknob.DrawValueKnob("Battery I", $"{info.DCi:0.0}A", info.DCiRate);


            ImGui.Separator();
            //Motor area
            ImGui.PushFont(bigNumberFont);
            ImGui.Text($"{info.RPMSpeed.RPM} RPM");
            ImGui.PopFont();
            barknob.DrawValueKnob("Motor", $"{info.Temp.ExternalTemp}°C", (float)info.Temp.ExternalTemp / (float)150);
            ImGui.SameLine();
            barknob.DrawValueKnob("Motor I", $"{info.MotorI:0.0}A", info.MotoriRate);

            NewWindowOffset();
            ImGui.End();

            if (info.NeedRequest)
            {
                Lev.Node.SendRequest(remote.ShortName.NodeID, (ushort)LC_Objects_Std.LC_Obj_DCSupply);
                Lev.Node.SendRequest(remote.ShortName.NodeID, (ushort)LC_Objects_Std.LC_Obj_MotorSupply);
                Lev.Node.SendRequest(remote.ShortName.NodeID, (ushort)LC_Objects_Std.LC_Obj_InternalVoltage);
                Lev.Node.SendRequest(remote.ShortName.NodeID, (ushort)LC_Objects_Std.LC_Obj_Temperature);
                Lev.Node.SendRequest(remote.ShortName.NodeID, (ushort)LC_Objects_Std.LC_Obj_RPM);
                Lev.Node.SendRequest(remote.ShortName.NodeID, (ushort)LC_Objects_Std.LC_Obj_ActiveFunctions);
            }
        }

        void DrawBMSWindow(LCRemoteNode remote, DeviceInfo info)
        {
            ImGui.SetNextWindowPos(positionOffset, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(new Vector2(250, 300));
            ImGui.Begin(remote.ToString() + $"###IDwindow{remote.ShortName.NodeID}", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse);
            ClampBorder();
            //Battery area
            ImGui.PushFont(bigNumberFont);
            ImGui.Text($"{info.DCSupply.Voltage / 1000.0f:0.00}V");
            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetWindowWidth() / 2);
            ImGui.Text($"{info.WattageFromSupply:0.}W");
            ImGui.PopFont();

            barknob.DrawValueKnob("Battery", $"{info.Temp.InternalTemp / 10.0f:0.0}°C", (float)info.Temp.InternalTemp / (float)1000);
            ImGui.SameLine();
            barknob.DrawValueKnob("Battery I", $"{info.DCi:0.0}A", info.DCiRate);

            ImGui.Separator();

            ImGui.Text($"Min: {info.CellMinMax.CellMin}mV");
            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetWindowWidth() / 2);
            ImGui.Text($"Max: {info.CellMinMax.CellMax}mV");

            float center = (info.CellMinMax.CellMax + info.CellMinMax.CellMin) / 2.0f / 1000.0f;
            float min = (info.CellMinMax.CellMin) / 1000.0f;
            int Ysize = (int)ImGui.GetContentRegionAvail().Y;
            if (min > center - Ysize / 1000.0f)
                min = center - Ysize / 1000.0f;
            float max = (info.CellMinMax.CellMax) / 1000.0f;
            if (max < center + Ysize / 1000.0f)
                max = center + Ysize / 1000.0f;

            ImGui.PlotHistogram("##Cells", ref info.CellsV_f[0], info.CellsV_f.Length, 0, "Cells", min, max, new Vector2(ImGui.GetContentRegionAvail().X, Ysize));

            NewWindowOffset();
            ImGui.End();

            if (info.NeedRequest)
            {
                Lev.Node.SendRequest(remote.ShortName.NodeID, (ushort)LC_Objects_Std.LC_Obj_DCSupply);
                Lev.Node.SendRequest(remote.ShortName.NodeID, (ushort)LC_Objects_Std.LC_Obj_InternalVoltage);
                Lev.Node.SendRequest(remote.ShortName.NodeID, (ushort)LC_Objects_Std.LC_Obj_Temperature);
                Lev.Node.SendRequest(remote.ShortName.NodeID, (ushort)LC_Objects_Std.LC_Obj_CellMinMax);
                Lev.Node.SendRequest(remote.ShortName.NodeID, (ushort)LC_Objects_Std.LC_Obj_CellsV);
            }
        }

        void DrawuLightWindow(LCRemoteNode remote, DeviceInfo info)
        {
            ImGui.SetNextWindowPos(positionOffset, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(sizeMin, sizeMax);
            ImGui.Begin(remote.ToString() + $"###IDwindow{remote.ShortName.NodeID}", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize);
            ClampBorder();
            //Battery area
            ImGui.Text($"{info.DCSupply.Voltage / 1000.0f:0.00}V");
            ImGui.SameLine();
            ImGui.Text($"{info.DCSupply.Current / 1000.0f:0.00}A");
            ImGui.Text($"{info.WattageFromSupply:0.0}W");

            ImGui.Text($"T1: {info.Temp.ExtraTemp1 / 10.0f:0.0} °C");
            ImGui.Text($"T2: {info.Temp.ExtraTemp2 / 10.0f:0.0} °C");
            ImGui.Text($"Internal: {info.Temp.InternalTemp / 10.0f:0.0} °C");

            NewWindowOffset();
            ImGui.End();

            if (info.NeedRequest)
            {
                Lev.Node.SendRequest(remote.ShortName.NodeID, (ushort)LC_Objects_Std.LC_Obj_DCSupply);
                //Lev.Node.SendRequest(remote.ShortName.NodeID, (ushort)LC_Objects_Std.LC_Obj_InternalVoltage);
                Lev.Node.SendRequest(remote.ShortName.NodeID, (ushort)LC_Objects_Std.LC_Obj_Temperature);
            }

        }

        void ClampBorder()
        {
            var Pos = ImGui.GetWindowPos();
            var Size = ImGui.GetWindowSize();
            bool needsClampToScreen = false;
            Vector2 targetPos = Pos;
            if (Pos.X < 0.0f)
            {
                needsClampToScreen = true;
                targetPos.X = 0.0f;
            }
            else if (Size.X + Pos.X > ImGui.GetWindowViewport().Size.X)
            {
                needsClampToScreen = true;
                targetPos.X = ImGui.GetWindowViewport().Size.X - Size.X;
            }
            if (Pos.Y < 0.0f)
            {
                needsClampToScreen = true;
                targetPos.Y = 0.0f;
            }
            else if (Size.Y + Pos.Y > ImGui.GetWindowViewport().Size.Y)
            {
                needsClampToScreen = true;
                targetPos.Y = ImGui.GetWindowViewport().Size.Y - Size.Y;
            }

            if (needsClampToScreen) // Necessary to prevent window from constantly undocking itself if docked.
            {
                ImGui.SetWindowPos(targetPos, ImGuiCond.Always);
            }

        }

        void NewWindowOffset()
        {
            Vector2 size = ImGui.GetWindowSize();
            positionOffset.X += size.X + 5;
            if (positionOffset.X > ImGui.GetWindowViewport().Size.X)
            {
                positionOffset.X = 0;
                positionOffset.Y += size.Y + 5;
            }
        }

    }

    class DeviceInfo
    {
        public LC_Obj_Supply_t DCSupply;
        public LC_Obj_Supply_t MotorSupply;
        public LC_Obj_InternalVoltage_t InterVoltage;
        public LC_Obj_Power_t Power;
        public LC_Obj_Temperature_t Temp;
        public LC_Obj_RPM_t RPMSpeed;
        public LC_Obj_CellMinMax_t CellMinMax;
        public LC_Obj_ActiveFunctions_t ActiveFunc;
        public float[] CellsV_f = new float[1];
        public short[] CellV
        {
            set
            {
                var cellsf = new float[value.Length];
                for (int i = 0; i < value.Length; i++)
                {
                    cellsf[i] = value[i] / 1000.0f;
                }
                CellsV_f = cellsf;
            }
        }
        public short CellsCount;

        public float DCi
        {
            get { return DCSupply.Current / 1000.0f; }
        }
        public float MotorI
        {
            get { return MotorSupply.Current / 1000.0f; }
        }

        public float InternalTemp
        {
            get { return Temp.InternalTemp / 1.0f; }
        }
        public float ExternalTemp
        {
            get { return Temp.ExternalTemp / 1.0f; }
        }
        public float WattageFromSupply
        {
            get { return (DCSupply.Current / 1000.0f) * (DCSupply.Voltage / 1000.0f); }
        }


        public int maxDCI = 50000;
        public int minDCI = -25000;
        public float DCiRate
        {
            get
            {
                if (maxDCI < DCSupply.Current)
                    maxDCI = DCSupply.Current;
                if (minDCI > DCSupply.Current)
                    minDCI = DCSupply.Current;
                float val = 0;
                if (DCSupply.Current >= 0)
                {
                    val = (float)DCSupply.Current / (float)maxDCI;
                }
                else
                {
                    val = (float)DCSupply.Current / (float)minDCI;
                }
                return val;
            }
        }

        public int maxMotorI = 100000;
        public int minMotorI = -50000;
        public float MotoriRate
        {
            get
            {
                if (maxMotorI < MotorSupply.Current)
                    maxMotorI = MotorSupply.Current;
                if (minMotorI > MotorSupply.Current)
                    minMotorI = MotorSupply.Current;
                float val = 0;
                if (MotorSupply.Current >= 0)
                {
                    val = (float)MotorSupply.Current / (float)maxMotorI;
                }
                else
                {
                    val = (float)MotorSupply.Current / (float)minMotorI;
                }
                return val;
            }
        }

        int fpsCounter = 60;
        public bool NeedRequest
        {
            get
            {
                fpsCounter++;
                if (fpsCounter > 60 / 10)
                {
                    fpsCounter = 0;
                    return true;
                }
                return false;
            }
        }
    }
}
