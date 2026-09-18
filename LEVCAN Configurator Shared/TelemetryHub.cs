using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using LEVCAN;

namespace LEVCAN_Configurator_Shared
{
    public class TelemetryHub
    {
        private readonly ConcurrentDictionary<ushort, NodeTelemetry> _nodesTelemetry = new();

        public event Action<ushort, NodeTelemetry>? TelemetryUpdated;
        public event Action<LC_Header, object>? StandardObjectReceived;

        public IReadOnlyDictionary<ushort, NodeTelemetry> AllTelemetry => _nodesTelemetry;

        public NodeTelemetry GetOrCreate(ushort nodeId)
        {
            return _nodesTelemetry.GetOrAdd(nodeId, id => new NodeTelemetry { NodeId = id });
        }

        public NodeTelemetry? Get(ushort nodeId)
        {
            _nodesTelemetry.TryGetValue(nodeId, out var telem);
            return telem;
        }

        public void ProcessMessage(LC_Header header, object data)
        {
            var telem = GetOrCreate(header.Source);
            telem.LastUpdated = DateTime.UtcNow;

            switch (header.MsgID)
            {
                case (ushort)LC_Objects_Std.LC_Obj_DCSupply:
                    telem.DCSupply = (LC_Obj_Supply_t)data;
                    break;
                case (ushort)LC_Objects_Std.LC_Obj_MotorSupply:
                    telem.MotorSupply = (LC_Obj_Supply_t)data;
                    break;
                case (ushort)LC_Objects_Std.LC_Obj_InternalVoltage:
                    telem.InternalVoltage = (LC_Obj_InternalVoltage_t)data;
                    break;
                case (ushort)LC_Objects_Std.LC_Obj_Power:
                    telem.Power = (LC_Obj_Power_t)data;
                    break;
                case (ushort)LC_Objects_Std.LC_Obj_Temperature:
                    telem.Temperature = (LC_Obj_Temperature_t)data;
                    break;
                case (ushort)LC_Objects_Std.LC_Obj_RPM:
                    telem.RPMSpeed = (LC_Obj_RPM_t)data;
                    break;
                case (ushort)LC_Objects_Std.LC_Obj_Speed:
                    telem.Speed = (LC_Obj_Speed_t)data;
                    break;
                case (ushort)LC_Objects_Std.LC_Obj_CellMinMax:
                    telem.CellMinMax = (LC_Obj_CellMinMax_t)data;
                    break;
                case (ushort)LC_Objects_Std.LC_Obj_ActiveFunctions:
                    telem.ActiveFunc = (LC_Obj_ActiveFunctions_t)data;
                    break;
                case (ushort)LC_Objects_Std.LC_Obj_CellsV:
                    if (data is byte[] databytes)
                    {
                        int length16b = (int)Math.Ceiling((float)databytes.Length / 2);
                        if (length16b >= 2)
                        {
                            short[] sdata = new short[length16b - 1];
                            Buffer.BlockCopy(databytes, 2, sdata, 0, databytes.Length - 2);
                            telem.CellsV = sdata;
                        }
                    }
                    break;
                case (ushort)LC_Objects_Std.LC_Obj_PowerModeIndex:
                    if (data is LC_Obj_PowerMode_t pmode)
                    {
                        int pidx = pmode.Index;
                        if (pidx < 0) pidx = 3;
                        else if (pidx > 2) pidx = 2;
                        else pidx -= 1;
                        if (pidx >= 0 && pidx < telem.PowerMode.Length)
                            telem.PowerMode[pidx] = pmode;
                    }
                    break;
            }

            StandardObjectReceived?.Invoke(header, data);
            TelemetryUpdated?.Invoke(header.Source, telem);
        }

        public void RequestTelemetry(LC_Node node, ushort targetNodeId)
        {
            if (node == null) return;
            node.SendRequest(targetNodeId, (ushort)LC_Objects_Std.LC_Obj_DCSupply);
            node.SendRequest(targetNodeId, (ushort)LC_Objects_Std.LC_Obj_MotorSupply);
            node.SendRequest(targetNodeId, (ushort)LC_Objects_Std.LC_Obj_InternalVoltage);
            node.SendRequest(targetNodeId, (ushort)LC_Objects_Std.LC_Obj_Temperature);
            node.SendRequest(targetNodeId, (ushort)LC_Objects_Std.LC_Obj_RPM);
            node.SendRequest(targetNodeId, (ushort)LC_Objects_Std.LC_Obj_Speed);
            node.SendRequest(targetNodeId, (ushort)LC_Objects_Std.LC_Obj_Power);
            node.SendRequest(targetNodeId, (ushort)LC_Objects_Std.LC_Obj_ActiveFunctions);
            node.SendRequest(targetNodeId, (ushort)LC_Objects_Std.LC_Obj_PowerModeIndex);
        }

        public void RegisterObjects(Action<LC_IObject> registerCallback)
        {
            var registeredIndices = new HashSet<ushort>();

            void Reg(LC_IObject obj)
            {
                registeredIndices.Add(obj.Index);
                registerCallback(obj);
            }

            Reg(new LC_ObjectFunction((ushort)LC_Objects_Std.LC_Obj_DCSupply, ProcessMessage, LC_ObjectAttributes.Writable, typeof(LC_Obj_Supply_t)));
            Reg(new LC_ObjectFunction((ushort)LC_Objects_Std.LC_Obj_MotorSupply, ProcessMessage, LC_ObjectAttributes.Writable, typeof(LC_Obj_Supply_t)));
            Reg(new LC_ObjectFunction((ushort)LC_Objects_Std.LC_Obj_InternalVoltage, ProcessMessage, LC_ObjectAttributes.Writable, typeof(LC_Obj_InternalVoltage_t)));
            Reg(new LC_ObjectFunction((ushort)LC_Objects_Std.LC_Obj_Power, ProcessMessage, LC_ObjectAttributes.Writable, typeof(LC_Obj_Power_t)));
            Reg(new LC_ObjectFunction((ushort)LC_Objects_Std.LC_Obj_Temperature, ProcessMessage, LC_ObjectAttributes.Writable, typeof(LC_Obj_Temperature_t)));
            Reg(new LC_ObjectFunction((ushort)LC_Objects_Std.LC_Obj_RPM, ProcessMessage, LC_ObjectAttributes.Writable, typeof(LC_Obj_RPM_t)));
            Reg(new LC_ObjectFunction((ushort)LC_Objects_Std.LC_Obj_Speed, ProcessMessage, LC_ObjectAttributes.Writable, typeof(LC_Obj_Speed_t)));
            Reg(new LC_ObjectFunction((ushort)LC_Objects_Std.LC_Obj_CellMinMax, ProcessMessage, LC_ObjectAttributes.Writable, typeof(LC_Obj_CellMinMax_t)));
            Reg(new LC_ObjectFunction((ushort)LC_Objects_Std.LC_Obj_CellsV, ProcessMessage, LC_ObjectAttributes.Writable, -194));
            Reg(new LC_ObjectFunction((ushort)LC_Objects_Std.LC_Obj_CellBalance, ProcessMessage, LC_ObjectAttributes.Writable, -32));
            Reg(new LC_ObjectFunction((ushort)LC_Objects_Std.LC_Obj_ActiveFunctions, ProcessMessage, LC_ObjectAttributes.Writable, typeof(LC_Obj_ActiveFunctions_t)));
            Reg(new LC_ObjectFunction((ushort)LC_Objects_Std.LC_Obj_PowerModeIndex, ProcessMessage, LC_ObjectAttributes.Writable, typeof(LC_Obj_PowerMode_t)));

            // Register remaining standard and system objects with dynamic byte array buffer
            foreach (var item in LevcanObjectCatalog.GetAll())
            {
                if (!registeredIndices.Contains(item.Index))
                {
                    Reg(new LC_ObjectFunction(item.Index, ProcessMessage, LC_ObjectAttributes.Writable, -512));
                }
            }
        }
    }
}
