using LEVCAN;
using LEVCAN.NET;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace LEVCAN_Configurator_Shared
{
    public enum CANDevice
    {
        Candle_USB,
        PCAN_USB,
        Nucular_USB2CAN,
        Null
    }

    public class LevcanHandler
    {
        public Icanbus icanPort;
        public LC_Node Node;
        List<LC_IObject> lc_objects = new List<LC_IObject>();

        public Dictionary<LCRemoteNode, LC_ParamClient> nodeParams = new Dictionary<LCRemoteNode, LC_ParamClient>();
        public List<LCRemoteNode> listOfRemotes = new List<LCRemoteNode>();
        public delegate void UpdateEvent(LC_Event_t evnt);
        public UpdateEvent UpdateEventHandler;
        public event Action? CommunicationActivity;
        public static Action? RequestWake;
        public LC_FileServer FileServer;
        public TelemetryHub Telemetry { get; } = new TelemetryHub();
        int DeviceBaudrate;
        public CANDevice CurrentDevice => olddevice;
        public int Baudrate => DeviceBaudrate;

        private readonly ConcurrentDictionary<(ushort NodeId, ushort ObjectIndex), TaskCompletionSource<ObjectDataResult>> _pendingDataRequests = new();

        public LevcanHandler(int baudrate, CANDevice cdevice = CANDevice.Nucular_USB2CAN, byte ownNodeId = 65)
        {
            //parse names
            var obj = new LC_ObjectString((ushort)LC_SystemMessage.NodeName, null, 128, LC_ObjectAttributes.Writable);
            obj.OnChange += nodename;
            lc_objects.Add(obj);
            //init node and load DLL
            DeviceBaudrate = baudrate;
            Node = new LC_Node(ownNodeId);
            //hardware
            DeviceSelect(cdevice);
            //objects
            lc_objects.Add(new LC_Events(Node, eventCallback));
            Telemetry.RegisterObjects(AddNodeObject);
            Telemetry.StandardObjectReceived += OnStandardObjectReceived;
            Node.Objects = lc_objects.ToArray();
            Node.AddressChanges += node_AddressChanges;
            //init client for parameters
            FileServer = new LC_FileServer(Node, Path.Combine(Directory.GetCurrentDirectory(), "files"));
            Node.StartNode();
        }

        public LevcanHandler(LC_Node existingNode, int baudrate = 1000000)
        {
            var obj = new LC_ObjectString((ushort)LC_SystemMessage.NodeName, null, 128, LC_ObjectAttributes.Writable);
            obj.OnChange += nodename;
            lc_objects.Add(obj);
            DeviceBaudrate = baudrate;
            Node = existingNode;
            olddevice = CANDevice.Null;
            lc_objects.Add(new LC_Events(Node, eventCallback));
            Telemetry.RegisterObjects(AddNodeObject);
            Telemetry.StandardObjectReceived += OnStandardObjectReceived;
            Node.Objects = lc_objects.ToArray();
            Node.AddressChanges += node_AddressChanges;
        }

        private void node_AddressChanges(object sender, AddressChangeArgs e)
        {
            CommunicationActivity?.Invoke();
            //happens when CAN have new node or it is gone
            lock (listOfRemotes)
            {
                switch (e.State)
                {
                    case LC_AddressState.New:
                        if (e.Index == listOfRemotes.Count)
                        {
                            //new item added
                            LCRemoteNode rnode = new LCRemoteNode(e.ShortName);
                            listOfRemotes.Add(rnode);
                        }
                        else if (e.Index < listOfRemotes.Count)
                        {
                            if (listOfRemotes[e.Index].ShortName.NodeID >= (ushort)LC_Address.Null)
                            {   //replace deleted one
                                listOfRemotes[e.Index].ShortName = e.ShortName;
                            }
                            else
                            {   //e.Index moved
                                listOfRemotes.Insert(e.Index, new LCRemoteNode(e.ShortName));
                            }
                        }

                        ushort reqId = e.ShortName.NodeID;
                        Task.Run(() => Node.SendRequest(reqId, LC_SystemMessage.NodeName));
                        break;

                    case LC_AddressState.Changed:
                        if (e.Index < listOfRemotes.Count)
                        {
                            lock (nodeParams)
                            {
                                if (nodeParams.ContainsKey(listOfRemotes[e.Index]))
                                    nodeParams.Remove(listOfRemotes[e.Index]);
                            }

                            LCRemoteNode rnode = new LCRemoteNode(e.ShortName);
                            listOfRemotes[e.Index] = rnode; //update
                        }
                        ushort chgId = e.ShortName.NodeID;
                        Task.Run(() => Node.SendRequest(chgId, LC_SystemMessage.NodeName));
                        break;

                    case LC_AddressState.Deleted:
                        for (int i = 0; i < listOfRemotes.Count; i++)
                        {
                            if (listOfRemotes[i].ShortName.NodeID == e.ShortName.NodeID)
                            {
                                lock (nodeParams)
                                {
                                    if (nodeParams.ContainsKey(listOfRemotes[i]))
                                        nodeParams.Remove(listOfRemotes[i]);
                                }
                                listOfRemotes.RemoveAt(i);
                            }
                        }
                        break;
                }
            }
            Debug.Print(e.State.ToString() + " node " + e.ShortName.NodeID);
        }

        private void eventCallback(LC_Event_t eventData)
        {
            UpdateEventHandler?.Invoke(eventData);
            CommunicationActivity?.Invoke();
        }

        void nodename(LC_Header hdr, string name)
        {
            lock (listOfRemotes)
            {
                var item = listOfRemotes.Cast<LCRemoteNode>().Where(p => p.ShortName.NodeID == hdr.Source);

                if (item.Count<LCRemoteNode>() > 0)
                {
                    var rnode = item.First<LCRemoteNode>();

                    if (rnode.Name == null || rnode.Name == "")
                    {
                        rnode.Name = name;
                    }
                }
            }
        }

        CANDevice olddevice = CANDevice.Null;
        public void DeviceSelect(CANDevice device)
        {
            if (olddevice == device)
                return;

            //new connection source selected;
            if (icanPort != null)
            {
                icanPort.FrameActivity -= OnFrameActivity;
                icanPort.Close();
            }
            switch (device)
            {
                case CANDevice.Nucular_USB2CAN:
                    //icanPort = new socketcand(node); //works as shit
                    icanPort = new NucularUSB2CAN(Node, DeviceBaudrate);
                    break;
                case CANDevice.PCAN_USB:
                    icanPort = new Pcanusb(Node, DeviceBaudrate);
                    break;
                case CANDevice.Candle_USB:
                    icanPort = new CandleUSB(Node, DeviceBaudrate);
                    break;
            }
            if (icanPort != null)
            {
                icanPort.FrameActivity += OnFrameActivity;
            }
            icanPort?.Open();
            olddevice = device;
        }

        private void OnFrameActivity()
        {
            CommunicationActivity?.Invoke();
        }

        public void DeviceSetBaudrate(int baudrate)
        {
            this.DeviceBaudrate = baudrate;
            icanPort.SetBaudrate(baudrate);
        }

        public LC_ParamClient? GetParametersClient(LCRemoteNode selected)
        {
            if (selected != null && selected.ShortName.NodeID >= (int)LC_Address.Null)
                return null;

            LC_ParamClient client = null;
            lock (nodeParams)
            {
                if (nodeParams.ContainsKey(selected))
                {//already here
                    client = nodeParams[selected];
                }
                else
                { //new key
                    client = new LC_ParamClient(Node, selected.ShortName.NodeID);
                    nodeParams.Add(selected, client); //save bind      
                }
            }
            return client;
        }

        public List<LCRemoteNode> GetRemoteNodesCopy()
        {
            lock (listOfRemotes)
            {
                return new List<LCRemoteNode>(listOfRemotes);
            }
        }

        public LCRemoteNode? FindRemoteNode(ushort nodeId)
        {
            lock (listOfRemotes)
            {
                return listOfRemotes.FirstOrDefault(r => r.ShortName.NodeID == nodeId);
            }
        }

        public LC_ParamClient? GetParametersClient(ushort nodeId)
        {
            var remote = FindRemoteNode(nodeId);
            if (remote == null)
            {
                var active = Node.GetActiveNodes();
                var match = active?.FirstOrDefault(a => a.NodeID == nodeId);
                if (match.HasValue && match.Value.NodeID == nodeId && nodeId < (ushort)LC_Address.Null)
                {
                    remote = new LCRemoteNode(match.Value);
                }
            }
            if (remote == null) return null;
            return GetParametersClient(remote);
        }

        public void AddNodeObject(LC_IObject lcobj)
        {
            lock (lc_objects)
            {
                lc_objects.Add(lcobj);
                Node.Objects = lc_objects.ToArray();
            }
        }

        public void EnsureObjectRegistered(ushort objectIndex)
        {
            lock (lc_objects)
            {
                if (!lc_objects.Any(o => o != null && o.Index == objectIndex))
                {
                    AddNodeObject(new LC_ObjectFunction(objectIndex, (header, data) => Telemetry.ProcessMessage(header, data), LC_ObjectAttributes.Writable, -512));
                }
            }
        }

        private void OnStandardObjectReceived(LC_Header header, object data)
        {
            var key = (header.Source, header.MsgID);
            if (_pendingDataRequests.TryGetValue(key, out var tcs))
            {
                byte[] bytes;
                if (data is byte[] b)
                    bytes = b;
                else if (data != null)
                {
                    int sz = Marshal.SizeOf(data.GetType());
                    bytes = new byte[sz];
                    IntPtr ptr = Marshal.AllocHGlobal(sz);
                    try
                    {
                        Marshal.StructureToPtr(data, ptr, false);
                        Marshal.Copy(ptr, bytes, 0, sz);
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(ptr);
                    }
                }
                else
                {
                    bytes = Array.Empty<byte>();
                }

                var decoded = LevcanObjectCatalog.DecodePayload(header.MsgID, bytes);

                var res = new ObjectDataResult
                {
                    NodeId = header.Source,
                    ObjectIndex = header.MsgID,
                    Success = true,
                    Size = bytes.Length,
                    DataBytes = bytes,
                    DataHex = BitConverter.ToString(bytes).Replace("-", " "),
                    Decoded = decoded
                };
                tcs.TrySetResult(res);
            }
        }

        public async Task<ObjectDataResult> RequestObjectDataAsync(ushort targetNode, ushort objectIndex, int timeoutMs = 1500, CancellationToken token = default)
        {
            EnsureObjectRegistered(objectIndex);

            var objInfo = LevcanObjectCatalog.Resolve(objectIndex.ToString())
                       ?? new LevcanObjectInfo { Id = $"Obj_{objectIndex}", Index = objectIndex };

            var remote = FindRemoteNode(targetNode);
            string nodeName = remote?.Name ?? $"Node_{targetNode}";

            var tcs = new TaskCompletionSource<ObjectDataResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            var key = (targetNode, objectIndex);
            _pendingDataRequests[key] = tcs;

            var sw = Stopwatch.StartNew();
            try
            {
                var ret = Node.SendRequest(targetNode, objectIndex);
                if (ret != LC_Return.Ok)
                {
                    return new ObjectDataResult
                    {
                        NodeId = targetNode,
                        NodeName = nodeName,
                        ObjectIndex = objectIndex,
                        ObjectId = objInfo.Id,
                        Success = false,
                        Error = $"Failed to send CAN request: {ret}"
                    };
                }

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                cts.CancelAfter(timeoutMs);

                using (cts.Token.Register(() => tcs.TrySetCanceled()))
                {
                    var result = await tcs.Task;
                    result.LatencyMs = Math.Round(sw.Elapsed.TotalMilliseconds, 1);
                    result.NodeName = nodeName;
                    result.ObjectId = objInfo.Id;
                    return result;
                }
            }
            catch (OperationCanceledException)
            {
                return new ObjectDataResult
                {
                    NodeId = targetNode,
                    NodeName = nodeName,
                    ObjectIndex = objectIndex,
                    ObjectId = objInfo.Id,
                    Success = false,
                    Error = $"Timeout waiting for response from node {targetNode} for object {objInfo.Id} ({objInfo.IndexHex})",
                    LatencyMs = Math.Round(sw.Elapsed.TotalMilliseconds, 1)
                };
            }
            finally
            {
                _pendingDataRequests.TryRemove(key, out _);
            }
        }

    }

    public class LCRemoteNode
    {
        public Encoding Encoding;
        public LC_NodeShortName ShortName;
        public string Name;

        public LCRemoteNode(LC_NodeShortName sname)
        {
            ShortName = sname;
            Encoding = sname.CodePage;
        }

        public override bool Equals(object? obj)
        {
            if (obj is LCRemoteNode other)
                return ShortName.NodeID == other.ShortName.NodeID;
            return false;
        }

        public override int GetHashCode()
        {
            return ShortName.NodeID.GetHashCode();
        }

        override public string ToString()
        {
            if (ShortName.NodeID > (ushort)LC_Address.Null)
                return "Invalid node";
            else
                return ShortName.NodeID.ToString() + ": " + Name;
        }
    }
}
