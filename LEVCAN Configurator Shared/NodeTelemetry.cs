using System;
using System.Collections.Generic;
using LEVCAN;

namespace LEVCAN_Configurator_Shared
{
    public class NodeTelemetry
    {
        public ushort NodeId { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        // Raw LEVCAN structures
        public LC_Obj_Supply_t? DCSupply { get; set; }
        public LC_Obj_Supply_t? MotorSupply { get; set; }
        public LC_Obj_InternalVoltage_t? InternalVoltage { get; set; }
        public LC_Obj_Power_t? Power { get; set; }
        public LC_Obj_Temperature_t? Temperature { get; set; }
        public LC_Obj_RPM_t? RPMSpeed { get; set; }
        public LC_Obj_Speed_t? Speed { get; set; }
        public LC_Obj_CellMinMax_t? CellMinMax { get; set; }
        public short[]? CellsV { get; set; }
        public LC_Obj_ActiveFunctions_t? ActiveFunc { get; set; }
        public LC_Obj_PowerMode_t[] PowerMode { get; } = new LC_Obj_PowerMode_t[4];

        // Calculated convenient values for API / JSON consumption
        public double AgeMs => (DateTime.UtcNow - LastUpdated).TotalMilliseconds;

        // Speed / RPM
        public int? RPM => RPMSpeed?.RPM;
        public int? ERPM => RPMSpeed?.ERPM;
        public short? SpeedValue => Speed?.Speed;

        // DC Supply
        public float? DcVoltage => DCSupply != null ? DCSupply.Value.Voltage / 1000.0f : null;
        public float? DcCurrent => DCSupply != null ? DCSupply.Value.Current / 1000.0f : null;
        public float? DcPowerWatts => (DcVoltage != null && DcCurrent != null) ? DcVoltage.Value * DcCurrent.Value : null;

        // Motor Supply
        public float? MotorVoltage => MotorSupply != null ? MotorSupply.Value.Voltage / 1000.0f : null;
        public float? MotorCurrent => MotorSupply != null ? MotorSupply.Value.Current / 1000.0f : null;

        // Power
        public int? Watts => Power?.Watts;

        // Temperatures in Celsius
        public float? ControllerTemp => Temperature != null ? Temperature.Value.InternalTemp / 1.0f : null;
        public float? MotorTemp => Temperature != null ? Temperature.Value.ExternalTemp / 1.0f : null;
        public float? ExtraTemp1 => Temperature != null ? Temperature.Value.ExtraTemp1 / 10.0f : null;
        public float? ExtraTemp2 => Temperature != null ? Temperature.Value.ExtraTemp2 / 10.0f : null;

        // Internal Voltages in Volts
        public float? Int12V => InternalVoltage != null ? InternalVoltage.Value.Int12V / 1000.0f : null;
        public float? Int5V => InternalVoltage != null ? InternalVoltage.Value.Int5V / 1000.0f : null;
        public float? Int3_3V => InternalVoltage != null ? InternalVoltage.Value.Int3_3V / 1000.0f : null;
        public float? IntREFV => InternalVoltage != null ? InternalVoltage.Value.IntREFV / 1000.0f : null;

        // Battery / Cell info
        public short? CellMinMv => CellMinMax?.CellMin;
        public short? CellMaxMv => CellMinMax?.CellMax;

        public Dictionary<string, object?> ToSummaryDictionary()
        {
            var dict = new Dictionary<string, object?>
            {
                ["nodeId"] = NodeId,
                ["lastUpdated"] = LastUpdated.ToString("o"),
                ["ageMs"] = Math.Round(AgeMs, 1),
                ["speed"] = new Dictionary<string, object?>
                {
                    ["rpm"] = RPM,
                    ["erpm"] = ERPM,
                    ["speed"] = SpeedValue
                },
                ["dcSupply"] = new Dictionary<string, object?>
                {
                    ["voltage"] = DcVoltage,
                    ["current"] = DcCurrent,
                    ["powerWatts"] = DcPowerWatts != null ? Math.Round(DcPowerWatts.Value, 2) : null
                },
                ["motorSupply"] = new Dictionary<string, object?>
                {
                    ["voltage"] = MotorVoltage,
                    ["current"] = MotorCurrent
                },
                ["temperatures"] = new Dictionary<string, object?>
                {
                    ["controller"] = ControllerTemp,
                    ["motor"] = MotorTemp,
                    ["extra1"] = ExtraTemp1,
                    ["extra2"] = ExtraTemp2
                },
                ["internalVoltage"] = new Dictionary<string, object?>
                {
                    ["v12"] = Int12V,
                    ["v5"] = Int5V,
                    ["v3_3"] = Int3_3V,
                    ["vRef"] = IntREFV
                },
                ["battery"] = new Dictionary<string, object?>
                {
                    ["cellMinMv"] = CellMinMv,
                    ["cellMaxMv"] = CellMaxMv,
                    ["cellsCount"] = CellsV?.Length ?? 0
                }
            };
            return dict;
        }
    }
}
