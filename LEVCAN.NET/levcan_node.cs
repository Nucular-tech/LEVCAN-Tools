using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LEVCAN
{
    

    internal class BCollectionSized : BlockingCollection<byte[]>
    {
        public uint ItemSize;
        public BCollectionSized(int length, uint itemsize) : base((int)itemsize)
        {
            ItemSize = itemsize;
        }
    }

    public class AddressChangeArgs : EventArgs
    {
        public LC_NodeShortName ShortName;
        public LC_AddressState State;
        public ushort Index;
    }

    unsafe public class LC_Node
    {
        [DllImport("LEVCANlib", EntryPoint = "LC_LibInit", CharSet = CharSet.Ansi)]
        private static extern IntPtr LC_LibInit();

        //[DllImport("LEVCANlib", EntryPoint = "LC_InitNodeDescriptor", CharSet = CharSet.Ansi)]
        //private static extern LC_Return lib_initNodeDescriptor(LC_NodeDescriptor* node);

        [DllImport("LEVCANlib", EntryPoint = "LC_CreateNode", CharSet = CharSet.Ansi)]
        private static extern LC_Return lib_createNode(LC_NodeDescriptor* node);

        [DllImport("LEVCANlib", EntryPoint = "LC_NetworkManager", CharSet = CharSet.Ansi)]
        private static extern void lib_networkManager(IntPtr node, uint time);

        [DllImport("LEVCANlib", EntryPoint = "LC_ReceiveManager", CharSet = CharSet.Ansi)]
        private static extern void lib_receiveManager(IntPtr node);

        [DllImport("LEVCANlib", EntryPoint = "LC_SendRequestSpec", CharSet = CharSet.Ansi)]
        private static extern LC_Return lib_sendRequestSpec(IntPtr node, ushort target, ushort index, byte size, byte TCP);

        [DllImport("LEVCANlib", EntryPoint = "LC_SendMessage", CharSet = CharSet.Ansi)]
        private static extern LC_Return lib_sendMessage(IntPtr node, ref lc_objectRecord obj, ushort index);

        [DllImport("LEVCANlib", EntryPoint = "LC_GetActiveNodes", CharSet = CharSet.Ansi)]
        private static extern LC_NodeShortName lib_getActiveNodes(IntPtr node, ref ushort position);

        [DllImport("LEVCANlib", EntryPoint = "LC_GetNode", CharSet = CharSet.Ansi)]
        private static extern LC_NodeShortName lib_getNode(ushort target);


        static List<LC_Node> nodes = new List<LC_Node>();

        internal readonly LC_NodeDescriptor* descriptor;
        public IntPtr DescriptorPtr { get { return (IntPtr)descriptor; } }
        public LC_NodeShortName ShortName { get { return descriptor->ShortName; } }
        public event EventHandler<AddressChangeArgs> AddressChanges;

        lc_object* objects_node;
        LC_IObject[] objects;

        public LC_Node(byte nodeID)
        {
            //setup load path, import DLL, reset path
            descriptor = (LC_NodeDescriptor*)LC_LibInit();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            descriptor->ShortName.CodePage = Encoding.GetEncoding(1251);
            descriptor->ShortName.NodeID = nodeID;
            descriptor->DeviceName = "LEVCAN PC library";
            descriptor->NodeName = "LEVCAN PC library";
            descriptor->VendorName = "Nucular.tech";
            descriptor->Serial1 = 1;
            descriptor->Serial2 = 2;
            descriptor->Serial3 = 3;
            descriptor->Serial4 = 4;

            nodes.Add(this);

            LC_Interface.SetAddressCallback(addressChanges);
        }

        private void addressChanges(LC_NodeShortName shortname, ushort index, LC_AddressState state)
        {
            AddressChangeArgs args = new AddressChangeArgs();
            args.ShortName = shortname;
            args.Index = index; 
            args.State = state;

            AddressChanges?.Invoke(this, args);
        }

        public void StartNode()
        {
            if (!LC_Interface.IsReady())
                throw new NullReferenceException("Initialize interface class first!");

            lib_createNode(descriptor);

            var updates = new Thread(nodeUpdate);
            updates.IsBackground = true;
            updates.Start();
            updates.Name = "Node update";

            var receive = new Thread(nodeReceive);
            receive.IsBackground = true;
            receive.Start();
            receive.Name = "Node receive";
        }

        void nodeUpdate()
        {
            while (true)
            {
                lib_networkManager(DescriptorPtr, 1);
                Thread.Sleep(1);
            }
        }

        void nodeReceive()
        {
            while (true)
            {
                lib_receiveManager(DescriptorPtr);
            }
        }

        public LC_IObject[] Objects
        {
            set
            {
                objects = value;
                var objToFree = objects_node;
                //alloc new obj
                ushort size = (ushort)value.Length;
                objects_node = (lc_object*)Marshal.AllocHGlobal(size * Marshal.SizeOf(typeof(lc_object)));
                //copy data
                for (int i = 0; i < size; i++)
                {
                    objects_node[i].Attributes = objects[i].Attributes;
                    if (objects[i].Index > (ushort)LC_SystemMessage.MaxMessageID)
                        throw new IndexOutOfRangeException("Index " + objects[i].Index.ToString() + " is out of range!");

                    objects_node[i].Index = objects[i].Index;
                    objects_node[i].Size = objects[i].Size;
                    objects_node[i].Address = (void*)objects[i].Pointer;

                }
                //assign obj list
                descriptor->ObjectsSize = 0;
                descriptor->objects = objects_node;
                descriptor->ObjectsSize = size;

                //clean up old
                if (objToFree != null)
                {
                    Marshal.FreeHGlobal((IntPtr)objToFree);
                }
            }
            get
            {
                return objects;
            }
        }

        public LC_Return SendRequest(ushort target, LC_SystemMessage index)
        {
            if (target > (ushort)LC_Address.Broadcast)
                throw new ArgumentOutOfRangeException("Target ID out of range!");

            return SendRequest((byte)target, (ushort)index, false);
        }
        public LC_Return SendRequest(byte target, ushort index, bool TCP)
        {
            if (target > (byte)LC_Address.Broadcast)
                throw new ArgumentOutOfRangeException("Target ID out of range!");

            return lib_sendRequestSpec(DescriptorPtr, target, index, 0, (byte)(TCP ? 1 : 0));
        }

        public LC_Return SendData(byte[] bytes, byte target, ushort index, bool TCP = false)
        {

            lc_objectRecord message = new lc_objectRecord();
            message.NodeID = target;
            message.Size = bytes.Length;
            message.Attributes = (ushort)lc_objectAttributes_internal.Cleanup;
            if (TCP)
                message.Attributes |= (ushort)lc_objectAttributes_internal.TCP;

            message.Address = (void*)Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, (IntPtr)message.Address, bytes.Length);
            return lib_sendMessage(DescriptorPtr, ref message, (ushort)index);
        }

        public LC_NodeShortName[] GetActiveNodes()
        {
            List<LC_NodeShortName> nodes = new List<LC_NodeShortName>();
            ushort position = 0;
            int i = 0;
            while (position < 120) // LEVCAN_MAX_TABLE_NODES
            {
                LC_NodeShortName sn = lib_getActiveNodes(DescriptorPtr, ref position);
                if (position < 120)
                    nodes.Add(sn);
            }
            return nodes.ToArray();
        }

        public LC_NodeShortName GetNodeShortName(ushort nodeID)
        {
            return lib_getNode(nodeID);
        }

        internal static LC_Node GetNodeByDesc(LC_NodeDescriptor* desc)
        {
            foreach (var node in nodes)
            {
                if (node.descriptor == desc)
                    return node;
            }
            throw new NullReferenceException("Node not found in list.");
        }

        public Encoding GetNodeEncoding(ushort id)
        {

            var sname = GetNodeShortName(id);
            if (sname.NodeID == (ushort)LC_Address.Broadcast)
                return ShortName.CodePage;//not found, use own codepage
            else
                return sname.CodePage; //use sender codepage to decode
        }
    }
    public interface IObjectHandler
    {
        object Value { get; set; }
    }
}
