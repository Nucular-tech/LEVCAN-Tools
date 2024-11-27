using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LEVCAN
{
    public enum LC_SystemMessage
    {
        AddressClaimed = 0x380,
        ComandedAddress,
        NodeName = 0x388,
        DeviceName,
        VendorName,
        VendorCode,
        HWVersion,
        SWVersion,
        SerialNumber,
        Parameters, //old parameters
        Variables,
        Events,
        Trace,
        DateTime,
        SWUpdate,
        Shutdown,
        FileServer,
        FileClient,
        SaveData,
        ParametersRequest,
        ParametersData,
        ParametersDescriptor,
        ParametersName,
        ParametersText,
        ParametersValue,
        End,
        MaxMessageID = 1023
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LC_Sys_DateTime_t
    {
        public byte Hour; //24H
        public byte Minute;
        public byte Second;
        public byte WeekDay; //0=Monday ... 6=Sunday
        public byte Date;
        public byte Month;
        public ushort Year;

        public LC_Sys_DateTime_t(DateTime time)
        {
            Year = (ushort)time.Year;
            Month = (byte)time.Month;
            Date = (byte)time.Day;
            WeekDay = (byte)(((int)time.DayOfWeek - 1 < 0) ? 6 : (int)time.DayOfWeek - 1);
            Second = (byte)time.Second;
            Minute = (byte)time.Minute;
            Hour = (byte)time.Hour;
        }

        public DateTime ToDateTime()
        {
            return new DateTime(Year, Month, Date, Hour, Minute, Second);
        }
    };
}
