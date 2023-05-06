using LEVCAN;
using LEVCAN.NET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Windows.Forms.AxHost;

namespace LEVCAN_Configurator
{
    enum CANDevice
    {
        Nucular_USB2CAN,
        PCAN_USB,
        Null
    }

    internal class LevcanHandler
    {
        public Icanbus icanPort;
        public LC_Node Node;
        List<LC_IObject> lc_objects = new List<LC_IObject>();

        public Dictionary<LCRemoteNode, LC_ParamClient> nodeParams = new Dictionary<LCRemoteNode, LC_ParamClient>();
        public List<LCRemoteNode> listOfRemotes = new List<LCRemoteNode>();
        public List<EventMessage> listOfEvents = new List<EventMessage>();
        public LC_FileServer FileServer;

        public LevcanHandler(CANDevice cdevice = CANDevice.Nucular_USB2CAN)
        {
            //objects[0] = new LC_Object((ushort)LC_SystemMessage.NodeName, new LC_Obj_ThrottleV_t(), LC_ObjectAttributes.Writable);
            var obj = new LC_ObjectString((ushort)LC_SystemMessage.NodeName, null, 128, LC_ObjectAttributes.Writable);
            obj.OnChange += nodename;
            lc_objects.Add(obj);

            //init node and load DLL
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
            //find existing event
            for (int i = 0; i < listOfEvents.Count; i++)
            {
                if (listOfEvents[i].Sender == eventData.Sender)
                { //replace
                    listOfEvents[i].UpdateMessage(eventData);
                    return;
                }
            }
            //add new
            listOfEvents.Add(new EventMessage(eventData));
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
                    icanPort = new NucularUSB2CAN(Node);
                    break;
                case CANDevice.PCAN_USB:
                    icanPort = new Pcanusb(Node);
                    break;
            }
            icanPort.Open();
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

    class LCRemoteNode
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
                return ShortName.NodeID.ToString() + " : " + Name;
        }
    }
}
