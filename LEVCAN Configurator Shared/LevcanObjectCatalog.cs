using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using LEVCAN;

namespace LEVCAN_Configurator_Shared
{
    public class LevcanObjectInfo
    {
        public string Id { get; set; } = "";
        public string RawName { get; set; } = "";
        public ushort Index { get; set; }
        public string IndexHex => $"0x{Index:X4}";
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public string? StructType { get; set; }
        public int? ExpectedSize { get; set; }
    }

    public class ObjectDataResult
    {
        public ushort NodeId { get; set; }
        public string? NodeName { get; set; }
        public ushort ObjectIndex { get; set; }
        public string ObjectIndexHex => $"0x{ObjectIndex:X4}";
        public string ObjectId { get; set; } = "";
        public bool Success { get; set; }
        public string? Error { get; set; }
        public int Size { get; set; }
        public string DataHex { get; set; } = "";
        public byte[] DataBytes { get; set; } = Array.Empty<byte>();
        public Dictionary<string, object?>? Decoded { get; set; }
        public double LatencyMs { get; set; }
    }

    public static class LevcanObjectCatalog
    {
        private static readonly List<LevcanObjectInfo> _catalog = new();
        private static readonly Dictionary<ushort, LevcanObjectInfo> _byIndex = new();
        private static readonly Dictionary<string, LevcanObjectInfo> _byId = new(StringComparer.OrdinalIgnoreCase);

        static LevcanObjectCatalog()
        {
            // Standard Objects (0x0300 - 0x0330)
            AddStd(LC_Objects_Std.LC_Obj_State, "State", "Device operating state", "int", 4);
            AddStd(LC_Objects_Std.LC_Obj_DCSupply, "DCSupply", "Controller battery power input (voltage mV, current mA)", "LC_Obj_Supply_t", 8);
            AddStd(LC_Objects_Std.LC_Obj_MotorSupply, "MotorSupply", "Motor power from controller (voltage mV, current mA)", "LC_Obj_Supply_t", 8);
            AddStd(LC_Objects_Std.LC_Obj_InternalVoltage, "InternalVoltage", "Internal power rails (12V, 5V, 3.3V, Vref in mV)", "LC_Obj_InternalVoltage_t", 8);
            AddStd(LC_Objects_Std.LC_Obj_Power, "Power", "Total electrical power in Watts and flow direction", "LC_Obj_Power_t", 5);
            AddStd(LC_Objects_Std.LC_Obj_Temperature, "Temperature", "Internal, external, and auxiliary temperatures (deg C)", "LC_Obj_Temperature_t", 8);
            AddStd(LC_Objects_Std.LC_Obj_RPM, "RPM", "Mechanical RPM and electrical ERPM", "LC_Obj_RPM_t", 8);
            AddStd(LC_Objects_Std.LC_Obj_RadSec, "RadSec", "Motor angular velocity in radians per second (float)", "LC_Obj_RadSec_t", 4);
            AddStd(LC_Objects_Std.LC_Obj_Speed, "Speed", "Vehicle wheel speed (km/h or m/s)", "LC_Obj_Speed_t", 2);
            AddStd(LC_Objects_Std.LC_Obj_ThrottleV, "ThrottleV", "Raw throttle analog voltage input", "LC_Obj_ThrottleV_t", 2);
            AddStd(LC_Objects_Std.LC_Obj_BrakeV, "BrakeV", "Raw brake analog voltage input", "LC_Obj_BrakeV_t", 2);
            AddStd(LC_Objects_Std.LC_Obj_ControlFactor, "ControlFactor", "Normalized throttle/brake control factor (float)", "LC_Obj_ControlFactor_t", 4);
            AddStd(LC_Objects_Std.LC_Obj_SpeedCommand, "SpeedCommand", "Target speed setpoint / command", "LC_Obj_SpeedCommand_t", 2);
            AddStd(LC_Objects_Std.LC_Obj_TorqueCommand, "TorqueCommand", "Target torque setpoint / command", "LC_Obj_TorqueCommand_t", 2);
            AddStd(LC_Objects_Std.LC_Obj_Buttons, "Buttons", "Button input states and bitmask", "LC_Obj_Buttons_t", 4);
            AddStd(LC_Objects_Std.LC_Obj_WhUsed, "WhUsed", "Energy consumed from battery (Watt-hours)", "LC_Obj_WhUsed_t", 4);
            AddStd(LC_Objects_Std.LC_Obj_WhStored, "WhStored", "Energy regenerated / stored (Watt-hours)", "LC_Obj_WhStored_t", 4);
            AddStd(LC_Objects_Std.LC_Obj_Distance, "Distance", "Odometer / trip distance travelled", "LC_Obj_Distance_t", 4);
            AddStd(LC_Objects_Std.LC_Obj_MotorHalls, "MotorHalls", "Motor Hall sensor states and angle", "LC_Obj_MotorHalls_t", 2);
            AddStd(LC_Objects_Std.LC_Obj_CellsV, "CellsV", "Battery cell individual voltages array", "short[]", null);
            AddStd(LC_Objects_Std.LC_Obj_CellMinMax, "CellMinMax", "Battery minimum and maximum cell voltages and delta", "LC_Obj_CellMinMax_t", 8);
            AddStd(LC_Objects_Std.LC_Obj_CellBalance, "CellBalance", "Battery cell balancing bitmask", "byte[]", null);
            AddStd(LC_Objects_Std.LC_Obj_UserActivity, "UserActivity", "Pedal / user input activity indicator", "LC_Obj_UserActivity_t", 2);
            AddStd(LC_Objects_Std.LC_Obj_ActiveFunctions, "ActiveFunctions", "Active controller modes / function flags bitfield", "LC_Obj_ActiveFunctions_t", 4);
            AddStd(LC_Objects_Std.LC_Obj_LightSensor, "LightSensor", "Ambient light sensor reading", "LC_Obj_LightSensor_t", 2);
            AddStd(LC_Objects_Std.LC_Obj_AccelerometerRaw, "AccelerometerRaw", "Raw 3-axis accelerometer readings", "LC_Obj_AccelerometerRaw_t", 6);
            AddStd(LC_Objects_Std.LC_Obj_Accelerometer, "Accelerometer", "Calibrated 3-axis acceleration vector", "LC_Obj_Accelerometer_t", 12);
            AddStd(LC_Objects_Std.LC_Obj_ControlFactorInt, "ControlFactorInt", "Integer control factor percentage (0-10000)", "int", 4);
            AddStd(LC_Objects_Std.LC_Obj_DCLimitIFactor, "DCLimitIFactor", "DC current limit reduction factor", "LC_Obj_DCLimit_t", 4);
            AddStd(LC_Objects_Std.LC_Obj_DCLimitIValue, "DCLimitIValue", "DC current limit value", "int", 4);
            AddStd(LC_Objects_Std.LC_Obj_DCLimitVValue, "DCLimitVValue", "DC voltage limit value", "int", 4);
            AddStd(LC_Objects_Std.LC_Obj_FOCstateV, "FOCstateV", "FOC voltage vectors (Vd, Vq)", "LC_Obj_FOCstateV_t", 8);
            AddStd(LC_Objects_Std.LC_Obj_FOCstateI, "FOCstateI", "FOC current vectors (Id, Iq)", "LC_Obj_FOCstateI_t", 8);
            AddStd(LC_Objects_Std.LC_Obj_FOCreqest, "FOCrequest", "FOC target current requests (Id, Iq)", "LC_Obj_FOCrequest_t", 8);
            AddStd(LC_Objects_Std.LC_Obj_AhUsed, "AhUsed", "Ampere-hours consumed", "LC_Obj_AhUsed_t", 4);
            AddStd(LC_Objects_Std.LC_Obj_AhStored, "AhStored", "Ampere-hours charged / regenerated", "LC_Obj_AhStored_t", 4);
            AddStd(LC_Objects_Std.LC_Obj_BatterySupply, "BatterySupply", "BMS battery pack output supply (voltage mV, current mA)", "LC_Obj_Supply_t", 8);
            AddStd(LC_Objects_Std.LC_Obj_AuxSupply, "AuxSupply", "Auxiliary 12V supply (voltage mV, current mA)", "LC_Obj_Supply_t", 8);
            AddStd(LC_Objects_Std.LC_Obj_ClimateSupply, "ClimateSupply", "Climate control supply (voltage mV, current mA)", "LC_Obj_Supply_t", 8);
            AddStd(LC_Objects_Std.LC_Obj_ACSupply, "ACSupply", "Single phase AC grid power supply", "LC_Obj_Supply_t", 8);
            AddStd(LC_Objects_Std.LC_Obj_ACSupply3Ph, "ACSupply3Ph", "Three phase AC grid power supply", "LC_Obj_Supply3ph_t", 24);
            AddStd(LC_Objects_Std.LC_Obj_SelectedPowerMode, "SelectedPowerMode", "Currently selected power mode / profile", "LC_Obj_PowerMode_t", null);
            AddStd(LC_Objects_Std.LC_Obj_PowerModeIndex, "PowerModeIndex", "Power mode configuration index and parameters", "LC_Obj_PowerMode_t", null);
            AddStd(LC_Objects_Std.LC_Obj_BatteryCurrents, "BatteryCurrents", "Multichannel battery currents", "int[]", null);
            AddStd(LC_Objects_Std.LC_Obj_BatteryVoltages, "BatteryVoltages", "Multichannel battery voltages", "int[]", null);
            AddStd(LC_Objects_Std.LC_Obj_ControlDirection, "ControlDirection", "Direction of drive / reverse", "byte", 1);
            AddStd(LC_Objects_Std.LC_Obj_PowerModeLimits, "PowerModeLimits", "Power mode maximum limit boundaries", "LC_Obj_PowerMode_t", null);

            // System Messages (0x0380 - 0x0398)
            AddSys(LC_SystemMessage.NodeName, "NodeName", "Node broadcast identification name", "string", null);
            AddSys(LC_SystemMessage.DeviceName, "DeviceName", "Device model / hardware marketing name", "string", null);
            AddSys(LC_SystemMessage.VendorName, "VendorName", "Manufacturer / vendor name string", "string", null);
            AddSys(LC_SystemMessage.VendorCode, "VendorCode", "Manufacturer numerical vendor code", "uint", 4);
            AddSys(LC_SystemMessage.HWVersion, "HWVersion", "Hardware revision string", "string", null);
            AddSys(LC_SystemMessage.SWVersion, "SWVersion", "Software / firmware revision string", "string", null);
            AddSys(LC_SystemMessage.SerialNumber, "SerialNumber", "Unique device serial number string", "string", null);
            AddSys(LC_SystemMessage.DateTime, "DateTime", "System RTC date and time", "LC_Sys_DateTime_t", 8);
            AddSys(LC_SystemMessage.Variables, "Variables", "Standard parameters / variables access", null, null);
            AddSys(LC_SystemMessage.Events, "Events", "Event and alert notifications stream", null, null);
            AddSys(LC_SystemMessage.Trace, "Trace", "Debug log / trace message stream", "string", null);
            AddSys(LC_SystemMessage.Shutdown, "Shutdown", "Command node to power down or reboot", null, 0);
            AddSys(LC_SystemMessage.SaveData, "SaveData", "Command node to commit non-volatile settings", null, 0);
        }

        private static void AddStd(LC_Objects_Std std, string cleanName, string description, string? structType, int? size)
        {
            var info = new LevcanObjectInfo
            {
                Id = cleanName,
                RawName = std.ToString(),
                Index = (ushort)std,
                Category = "StandardObject",
                Description = description,
                StructType = structType,
                ExpectedSize = size
            };
            _catalog.Add(info);
            _byIndex[info.Index] = info;
            _byId[info.Id] = info;
            _byId[info.RawName] = info;
        }

        private static void AddSys(LC_SystemMessage sys, string cleanName, string description, string? structType, int? size)
        {
            var info = new LevcanObjectInfo
            {
                Id = cleanName,
                RawName = sys.ToString(),
                Index = (ushort)sys,
                Category = "SystemMessage",
                Description = description,
                StructType = structType,
                ExpectedSize = size
            };
            _catalog.Add(info);
            _byIndex[info.Index] = info;
            _byId[info.Id] = info;
            _byId[info.RawName] = info;
        }

        public static IReadOnlyList<LevcanObjectInfo> GetAll() => _catalog;

        public static IReadOnlyList<LevcanObjectInfo> Filter(string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return _catalog;

            string q = query.Trim().TrimStart('?');
            if (string.IsNullOrWhiteSpace(q))
                return _catalog;

            // Check if query is in key=val format like "filter=speed" or "q=speed"
            int eqIdx = q.IndexOf('=');
            if (eqIdx >= 0 && eqIdx < q.Length - 1)
            {
                q = q.Substring(eqIdx + 1).Trim();
            }

            return _catalog.Where(item =>
                item.Id.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                item.RawName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                item.Description.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                item.Category.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                item.IndexHex.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                item.Index.ToString().Equals(q, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        public static LevcanObjectInfo? Resolve(string idOrNumber)
        {
            if (string.IsNullOrWhiteSpace(idOrNumber))
                return null;

            string trimmed = idOrNumber.Trim();

            // Direct ID or RawName lookup
            if (_byId.TryGetValue(trimmed, out var exact))
                return exact;

            // Try clean name without common prefixes
            if (trimmed.StartsWith("LC_Obj_", StringComparison.OrdinalIgnoreCase))
            {
                string stripped = trimmed.Substring(7);
                if (_byId.TryGetValue(stripped, out var foundStripped))
                    return foundStripped;
            }

            // Hex lookup: 0x308 or 0x0308
            if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                if (ushort.TryParse(trimmed.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort hexVal))
                {
                    if (_byIndex.TryGetValue(hexVal, out var byHex))
                        return byHex;

                    return new LevcanObjectInfo
                    {
                        Id = $"Obj_0x{hexVal:X4}",
                        RawName = $"Index_{hexVal}",
                        Index = hexVal,
                        Category = "Custom",
                        Description = $"Custom LEVCAN object index {hexVal} (0x{hexVal:X4})"
                    };
                }
            }

            // Decimal integer lookup
            if (ushort.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort decVal))
            {
                if (_byIndex.TryGetValue(decVal, out var byDec))
                    return byDec;

                return new LevcanObjectInfo
                {
                    Id = $"Obj_{decVal}",
                    RawName = $"Index_{decVal}",
                    Index = decVal,
                    Category = "Custom",
                    Description = $"Custom LEVCAN object index {decVal} (0x{decVal:X4})"
                };
            }

            // Fuzzy prefix/contains fallback
            var matches = Filter(trimmed);
            return matches.FirstOrDefault();
        }

        public static Dictionary<string, object?>? DecodePayload(ushort objectIndex, byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return null;

            var dict = new Dictionary<string, object?>();

            try
            {
                switch (objectIndex)
                {
                    case (ushort)LC_Objects_Std.LC_Obj_Speed:
                        if (bytes.Length >= 2)
                        {
                            short speed = BitConverter.ToInt16(bytes, 0);
                            dict["speed"] = speed;
                        }
                        break;

                    case (ushort)LC_Objects_Std.LC_Obj_RPM:
                        if (bytes.Length >= 8)
                        {
                            dict["rpm"] = BitConverter.ToInt32(bytes, 0);
                            dict["erpm"] = BitConverter.ToInt32(bytes, 4);
                        }
                        break;

                    case (ushort)LC_Objects_Std.LC_Obj_RadSec:
                        if (bytes.Length >= 4)
                        {
                            dict["radSec"] = BitConverter.ToSingle(bytes, 0);
                        }
                        break;

                    case (ushort)LC_Objects_Std.LC_Obj_DCSupply:
                    case (ushort)LC_Objects_Std.LC_Obj_MotorSupply:
                    case (ushort)LC_Objects_Std.LC_Obj_BatterySupply:
                    case (ushort)LC_Objects_Std.LC_Obj_AuxSupply:
                    case (ushort)LC_Objects_Std.LC_Obj_ClimateSupply:
                    case (ushort)LC_Objects_Std.LC_Obj_ACSupply:
                        if (bytes.Length >= 8)
                        {
                            int vMv = BitConverter.ToInt32(bytes, 0);
                            int iMa = BitConverter.ToInt32(bytes, 4);
                            double v = vMv / 1000.0;
                            double a = iMa / 1000.0;
                            dict["voltage"] = Math.Round(v, 3);
                            dict["current"] = Math.Round(a, 3);
                            dict["powerWatts"] = Math.Round(v * a, 1);
                            dict["voltageRawMv"] = vMv;
                            dict["currentRawMa"] = iMa;
                        }
                        break;

                    case (ushort)LC_Objects_Std.LC_Obj_InternalVoltage:
                        if (bytes.Length >= 8)
                        {
                            dict["v12"] = Math.Round(BitConverter.ToInt16(bytes, 0) / 1000.0, 3);
                            dict["v5"] = Math.Round(BitConverter.ToInt16(bytes, 2) / 1000.0, 3);
                            dict["v3_3"] = Math.Round(BitConverter.ToInt16(bytes, 4) / 1000.0, 3);
                            dict["vRef"] = Math.Round(BitConverter.ToInt16(bytes, 6) / 1000.0, 3);
                        }
                        break;

                    case (ushort)LC_Objects_Std.LC_Obj_Power:
                        if (bytes.Length >= 4)
                        {
                            dict["watts"] = BitConverter.ToInt32(bytes, 0);
                            if (bytes.Length > 4)
                                dict["direction"] = bytes[4];
                        }
                        break;

                    case (ushort)LC_Objects_Std.LC_Obj_Temperature:
                        if (bytes.Length >= 8)
                        {
                            dict["controller"] = BitConverter.ToInt16(bytes, 0);
                            dict["motor"] = BitConverter.ToInt16(bytes, 2);
                            dict["extra1"] = Math.Round(BitConverter.ToInt16(bytes, 4) / 10.0, 1);
                            dict["extra2"] = Math.Round(BitConverter.ToInt16(bytes, 6) / 10.0, 1);
                        }
                        break;

                    case (ushort)LC_Objects_Std.LC_Obj_ActiveFunctions:
                        if (bytes.Length >= 4)
                        {
                            dict["activeFunctions"] = BitConverter.ToUInt32(bytes, 0);
                        }
                        break;

                    case (ushort)LC_SystemMessage.DateTime:
                        if (bytes.Length >= 8)
                        {
                            dict["hour"] = bytes[0];
                            dict["minute"] = bytes[1];
                            dict["second"] = bytes[2];
                            dict["weekday"] = bytes[3];
                            dict["day"] = bytes[4];
                            dict["month"] = bytes[5];
                            dict["year"] = BitConverter.ToUInt16(bytes, 6);
                        }
                        break;

                    case (ushort)LC_SystemMessage.NodeName:
                    case (ushort)LC_SystemMessage.DeviceName:
                    case (ushort)LC_SystemMessage.VendorName:
                    case (ushort)LC_SystemMessage.HWVersion:
                    case (ushort)LC_SystemMessage.SWVersion:
                    case (ushort)LC_SystemMessage.SerialNumber:
                        string text = Encoding.UTF8.GetString(bytes).TrimEnd('\0').Trim();
                        dict["text"] = text;
                        break;

                    default:
                        // If all bytes printable ASCII, include text representation
                        if (bytes.All(b => b >= 32 && b <= 126))
                        {
                            dict["ascii"] = Encoding.ASCII.GetString(bytes);
                        }
                        break;
                }
            }
            catch
            {
                // Fallback on error
            }

            return dict.Count > 0 ? dict : null;
        }
    }
}
