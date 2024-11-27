using LEVCAN;
using LEVCAN.NET;
using System.Diagnostics;
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
        public LC_FileServer FileServer;
        int DeviceBaudrate;
        public LevcanHandler(int baudrate, CANDevice cdevice = CANDevice.Nucular_USB2CAN)
        {
            //parse names
            var obj = new LC_ObjectString((ushort)LC_SystemMessage.NodeName, null, 128, LC_ObjectAttributes.Writable);
            obj.OnChange += nodename;
            lc_objects.Add(obj);
            //init node and load DLL
            DeviceBaudrate = baudrate;
            Node = new LC_Node(65);
            //hardware
            DeviceSelect(cdevice);
            //objects
            lc_objects.Add(new LC_Events(Node, eventCallback));
            Node.Objects = lc_objects.ToArray();
            Node.AddressChanges += node_AddressChanges;
            //init client for parameters
            FileServer = new LC_FileServer(Node, Path.Combine(Directory.GetCurrentDirectory(), "files"));
            Node.StartNode();
        }

        private void node_AddressChanges(object sender, AddressChangeArgs e)
        {
            //happens when CAN have new node or it is gone
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

                    Node.SendRequest(e.ShortName.NodeID, LC_SystemMessage.NodeName);
                    break;

                case LC_AddressState.Changed:
                    if (e.Index < listOfRemotes.Count)
                    {
                        if (nodeParams.ContainsKey(listOfRemotes[e.Index]))
                            nodeParams.Remove(listOfRemotes[e.Index]);

                        LCRemoteNode rnode = new LCRemoteNode(e.ShortName);
                        listOfRemotes[e.Index] = rnode; //update
                    }
                    Node.SendRequest(e.ShortName.NodeID, LC_SystemMessage.NodeName);
                    break;

                case LC_AddressState.Deleted:
                    for (int i = 0; i < listOfRemotes.Count; i++)
                    {
                        if (listOfRemotes[i].ShortName.NodeID == e.ShortName.NodeID)
                        {
                            if (nodeParams.ContainsKey(listOfRemotes[i]))
                                nodeParams.Remove(listOfRemotes[i]);
                            listOfRemotes.RemoveAt(i);
                        }
                    }
                    break;
            }
            Debug.Print(e.State.ToString() + " node " + e.ShortName.NodeID);
        }

        private void eventCallback(LC_Event_t eventData)
        {
            UpdateEventHandler?.Invoke(eventData);
        }

        void nodename(LC_Header hdr, string name)
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

        CANDevice olddevice = CANDevice.Null;
        public void DeviceSelect(CANDevice device)
        {
            if (olddevice == device)
                return;

            //new connection source selected;
            if (icanPort != null)
                icanPort.Close();
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
            icanPort.Open();
        }

        public void DeviceSetBaudrate(int baudrate)
        {
            this.DeviceBaudrate = baudrate;
            icanPort.SetBaudrate(baudrate);
        }

        public LC_ParamClient GetParametersClient(LCRemoteNode selected)
        {
            if (selected != null && selected.ShortName.NodeID >= (int)LC_Address.Null)
                return null;

            LC_ParamClient client = null;
            if (nodeParams.ContainsKey(selected))
            {//already here
                client = nodeParams[selected];
            }
            else
            { //new key
                client = new LC_ParamClient(Node, selected.ShortName.NodeID, selected.Encoding);
                nodeParams.Add(selected, client); //save bind      
            }
            return client;
        }

        public void AddNodeObject(LC_IObject lcobj)
        {
            lc_objects.Add(lcobj);
            Node.Objects = lc_objects.ToArray();
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

        override public string ToString()
        {
            if (ShortName.NodeID > (ushort)LC_Address.Null)
                return "Invalid node";
            else
                return ShortName.NodeID.ToString() + ": " + Name;
        }
    }
}
