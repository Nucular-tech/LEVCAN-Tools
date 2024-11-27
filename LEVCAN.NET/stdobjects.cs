/*******************************************************************************
* LEVCAN: Light Electric Vehicle CAN protocol [LC]
* Copyright (C) 2020 Vasiliy Sukhoparov
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
******************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LEVCAN
{
    public enum LC_Device
    {
        LC_Device_Unknown = 0,
        LC_Device_reserved = 0x1,
        LC_Device_BMS = 0x2,
        LC_Device_Controller = 0x4,
        LC_Device_Display = 0x8,
        LC_Device_Light = 0x10,
        LC_Device_RemoteControl = 0x20,
        LC_Device_Misc = 0x40,
        LC_Device_Debug = 0x80,
        LC_Device_reserved2 = 0x100,
        LC_Device_reserved3 = 0x200,
    };

    public enum LC_Objects_Std
    {
        LC_Obj_State = 0x300,
        LC_Obj_DCSupply,
        LC_Obj_MotorSupply,
        LC_Obj_InternalVoltage,
        LC_Obj_Power,
        LC_Obj_Temperature,
        LC_Obj_RPM,
        LC_Obj_RadSec,
        LC_Obj_Speed,
        LC_Obj_ThrottleV,
        LC_Obj_BrakeV,
        LC_Obj_ControlFactor,
        LC_Obj_SpeedCommand,
        LC_Obj_TorqueCommand,
        LC_Obj_Buttons,
        LC_Obj_WhUsed,
        LC_Obj_WhStored,
        LC_Obj_Distance,
        LC_Obj_MotorHalls,
        LC_Obj_CellsV,
        LC_Obj_CellMinMax,
        LC_Obj_CellBalance,
        LC_Obj_UserActivity,
        LC_Obj_ActiveFunctions,
        LC_Obj_LightSensor,
        LC_Obj_AccelerometerRaw,
        LC_Obj_Accelerometer,
        LC_Obj_ControlFactorInt,
        LC_Obj_DCLimitIFactor,
        LC_Obj_DCLimitIValue,
        LC_Obj_DCLimitVValue,
        LC_Obj_FOCstateV,
        LC_Obj_FOCstateI,
        LC_Obj_FOCreqest,
        LC_Obj_AhUsed,
        LC_Obj_AhStored,
        LC_Obj_CFactor_Internal,
        LC_Obj_CFactorInt_Internal,
        LC_Obj_SelectedPowerMode,
        LC_Obj_PowerModeIndex,
        LC_Obj_BatteryCurrents,
        LC_Obj_BatteryVoltages,
        LC_Obj_ControlDirection,
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LC_Obj_Supply_t
    {
        public int Voltage; //mV
        public int Current; //mA
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LC_Obj_InternalVoltage_t
    {
        public short Int12V; //mV
        public short Int5V; //mV
        public short Int3_3V; //mV
        public short IntREFV; //mV
    };

    public enum Direction
    {
        LC_Obj_Power_Idle, LC_Obj_Power_Charging, LC_Obj_Power_Discharging
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LC_Obj_Power_t
    {
        public int Watts; //W
        public byte Direction;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LC_Obj_Temperature_t
    {
        public short InternalTemp; //Celsius degree
        public short ExternalTemp;
        public short ExtraTemp1;
        public short ExtraTemp2;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LC_Obj_RPM_t
    {
        public int RPM;
        public int ERPM;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LC_Obj_RadSec_t
    {
        public float RadSec;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LC_Obj_Speed_t
    {
        public short Speed;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LC_Obj_ThrottleV_t
    {
        public short ThrottleV; //mV
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LC_Obj_BrakeV_t
    {
        public short BrakeV; //mV
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LC_Obj_ControlFactor_t
    {
        public float ControlFactor; //positive - throttle, negative - brake, range: -1...1
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LC_Obj_ControlFactorInt_t
    {
        public ushort BrakeFactor; //100...10000
        public ushort ThrottleFactor; //100...10000
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LC_Obj_Buttons_t
    {
        public ushort buttons;
        public ushort extraButtons;

        public LC_Obj_Buttons_Bt Buttons
        {
            get { return (LC_Obj_Buttons_Bt)buttons; }
            set { buttons = (ushort)value; }
        }

        public LC_Obj_Buttons_Bt LC_Obj_Buttons_ExBt
        {
            get { return (LC_Obj_Buttons_Bt)extraButtons; }
            set { extraButtons = (ushort)value; }
        }
    }

    [Flags]
    public enum LC_Obj_Buttons_Bt
    {
        Enable = 1 << 0, //0
        Brake = 1 << 1, //1
        Lock = 1 << 2, //2
        Reverse = 1 << 3, //3
        Speed_N = 0,  //4-6
        Speed_1 = 1 << 4,
        Speed_2 = 2 << 4,
        Speed_3 = 3 << 4,
        Speed_Mask = 7 << 4,
        Speed_Offset = 4,
        Cruise = 1 << 7 //7
    }

    [Flags]
    public enum LC_Obj_Buttons_ExBt
    {
        ExButton1 = 1 << 0,
        ExButton2 = 1 << 1,
        ExButton3 = 1 << 2,
        ExButton4 = 1 << 3,
        ExButton5 = 1 << 4,
        ExButton6 = 1 << 5,
        ExButton7 = 1 << 6,
        ExButton8 = 1 << 7,
        ExButton9 = 1 << 8,
        ExButton10 = 1 << 9,
        ExButton11 = 1 << 10,
        ExButton12 = 1 << 11,
        ExButton13 = 1 << 12,
        ExButton14 = 1 << 13,
        ExButton15 = 1 << 14,
        ExButton16 = 1 << 15,
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LC_Obj_ActiveFunctions_t
    {
        ulong functions;
        public LC_Obj_ActiveFunctions_e Functions
        {
            get { return (LC_Obj_ActiveFunctions_e)functions; }
            set { functions = (ulong)value; }
        }
    }

    [Flags]
    public enum LC_Obj_ActiveFunctions_e
    {
        Enable = 1 << 0,
        Lock = 1 << 1,
        Throttle = 1 << 2,
        Brake = 1 << 3,
        Speed_N = 0,
        Speed_1 = 1 << 4,
        Speed_2 = 2 << 4,
        Speed_3 = 3 << 4,
        Reverse = 1 << 7,
        Cruise = 1 << 8,
        TurnRight = 1 << 9,
        TurnLeft = 1 << 10,
        LowBeam = 1 << 11,
        HighBeam = 1 << 12,
        PedalAssist = 1 << 13,
        ConverterMode = 1 << 14,
        WheelDriveLock = 1 << 15,
        BatteryUnlocked = 1 << 16,
        MotorWarning = 1 << 17,
        MotorFail = 1 << 18,
        ControllerWarning = 1 << 19,
        ControllerFail = 1 << 20,
        LowBattery = 1 << 21,
        BatteryWarning = 1 << 22,
        BatteryFail = 1 << 23,
        EBrakeLimited = 1 << 24,
        EABS = 1 << 25,
        EASR = 1 << 26,
        ESP = 1 << 27,
        CruiseReady = 1 << 28,
        Service = 1 << 29,
        FanActive = 1 << 30,
        BatteryHeater = 1 << 31,
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LC_Obj_WhUsed_t
    {
        public int WhUsed;
        public int WhUsedFromEn;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LC_Obj_WhStored_t
    {
        public int WhStored;
        public int WhTotalStorage;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LC_Obj_Distance_t
    {
        public uint TripMeterFromEn;
        public ushort TotalTripKm;  //odometer?
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LC_Obj_MotorHalls_t
    {
        public short HallA; //mV
        public short HallB; //mV
        public short HallC; //mV
        public byte InputDigital; //bits 1,2,3
        public byte HallState; //1..2..3..4..5..6
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LC_Obj_CellsV_t
    {
        public short Number;
        public short[] Cells; //mV
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LC_Obj_CellBalance_t
    {
        public byte Number; //number of bits
        public byte[] Balance; //bit field
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LC_Obj_CellMinMax_t
    {
        public short CellMin; //mV
        public short CellMax; //mV
        public short TempMin; //Celsius degree
        public short TempMax;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LC_Obj_LightSensor_t
    {
        public ushort Normalized; //0-1024
        public ushort SensorVoltage; //mV
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LC_Obj_Accelerometer_t
    {
        public short AxisX;
        public short AxisY;
        public short AxisZ;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LC_Obj_DCLimit_t
    {
        public ushort Charge; //factor 32767 = 1.0f
        public ushort Discharge; //factor 32767 = 1.0f
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LC_Obj_DCLimitGroup_t
    {
        public ushort Group;
        public ushort Charge; //factor 32767 = 1.0f
        public ushort Discharge; //factor 32767 = 1.0f
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LC_Obj_FOCstateV_t
    {
        public float Vd;
        public float Vq;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LC_Obj_FOCstateI_t
    {
        public float Iq;
        public float Id;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LC_Obj_FOCrequest_t
    {
        public float Iq_request;
        public float Id_request;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LC_Obj_AhUsed_t
    {
        public int mAhUsed;
        public int mAhUsedFromEn;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LC_Obj_AhStored_t
    {
        public int mAhStored;
        public int mAhTotalStorage;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LC_Obj_PowerMode_t
    {
        public ushort PhaseI;
        public ushort BatteryI; //factor 32767 = 1.0f
        public ushort Power; //factor 32767 = 1.0f
        public byte Speed; //number of bits
        public sbyte Index; //number of bits
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LC_Obj_BatteryCurrents_t
    {
        public uint ChargeI;
        public uint DischargeI;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LC_Obj_BatteryVoltages_t
    {
        public uint MinV;
        public uint MaxV;
    };
}
