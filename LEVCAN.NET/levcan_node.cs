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
    public class AddressChangeArgs : EventArgs
    {
        public LC_NodeShortName ShortName;
        public LC_AddressState State;
        public ushort Index;
    }

    unsafe public class LC_Node : IDisposable
    {
        [DllImport("LEVCANlib", EntryPoint = "LC_Node_Create", CharSet = CharSet.Ansi)]
        public static extern IntPtr LC_Node_Create(byte nodeID);

        [DllImport("LEVCANlib", EntryPoint = "LC_Node_Destroy", CharSet = CharSet.Ansi)]
        public static extern void LC_Node_Destroy(IntPtr node);

        [DllImport("LEVCANlib", EntryPoint = "LC_Node_SetIdentity", CharSet = CharSet.Ansi)]
        public static extern void LC_Node_SetIdentity(IntPtr node, string nodeName, string deviceName, string vendorName, ushort codePage, uint[] serial);

        [DllImport("LEVCANlib", EntryPoint = "LC_Node_SetObjects", CharSet = CharSet.Ansi)]
        public static extern void LC_Node_SetObjects(IntPtr node, IntPtr objects, ushort count);

        [DllImport("LEVCANlib", EntryPoint = "LC_Node_SetDirectories", CharSet = CharSet.Ansi)]
        public static extern void LC_Node_SetDirectories(IntPtr node, IntPtr directories, ushort count);

        [DllImport("LEVCANlib", EntryPoint = "LC_Node_GetShortName", CharSet = CharSet.Ansi)]
        public static extern LC_NodeShortName LC_Node_GetShortName(IntPtr node);

        [DllImport("LEVCANlib", EntryPoint = "LC_Node_SetShortName", CharSet = CharSet.Ansi)]
        public static extern void LC_Node_SetShortName(IntPtr node, LC_NodeShortName shortName);

        [DllImport("LEVCANlib", EntryPoint = "LC_Node_GetState", CharSet = CharSet.Ansi)]
        public static extern byte LC_Node_GetState(IntPtr node);

        [DllImport("LEVCANlib", EntryPoint = "LC_Node_SetAccessLevel", CharSet = CharSet.Ansi)]
        public static extern void LC_Node_SetAccessLevel(IntPtr node, byte accessLevel);

        [DllImport("LEVCANlib", EntryPoint = "LC_Node_GetAccessLevel", CharSet = CharSet.Ansi)]
        public static extern byte LC_Node_GetAccessLevel(IntPtr node);

        [DllImport("LEVCANlib", EntryPoint = "LC_LibInit", CharSet = CharSet.Ansi)]
        private static extern IntPtr LC_LibInit();

        [DllImport("LEVCANlib", EntryPoint = "LC_CreateNode", CharSet = CharSet.Ansi)]
        private static extern LC_Return lib_createNode(IntPtr node);

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

        [DllImport("LEVCANlib", EntryPoint = "LC_Malloc", CharSet = CharSet.Ansi)]
        public static extern IntPtr LC_Malloc(IntPtr size);

        [DllImport("LEVCANlib", EntryPoint = "LC_Free", CharSet = CharSet.Ansi)]
        public static extern void LC_Free(IntPtr ptr);


        static List<LC_Node> nodes = new List<LC_Node>();

        internal readonly IntPtr descriptor;
        public IntPtr DescriptorPtr { get { return descriptor; } }
        public LC_NodeShortName ShortName
        {
            get { return LC_Node_GetShortName(descriptor); }
            set { LC_Node_SetShortName(descriptor, value); }
        }
        public LC_NodeState State { get { return (LC_NodeState)LC_Node_GetState(descriptor); } }
        public byte AccessLevel
        {
            get { return LC_Node_GetAccessLevel(descriptor); }
            set { LC_Node_SetAccessLevel(descriptor, value); }
        }
        public event EventHandler<AddressChangeArgs> AddressChanges;

        lc_object* objects_node;
        LC_IObject[] objects;

        public LC_Node(byte nodeID)
        {
            descriptor = LC_Node_Create(nodeID);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            uint[] serial = new uint[] { 1, 2, 3, 4 };
            LC_Node_SetIdentity(descriptor, "LEVCAN PC library", "LEVCAN PC library", "Nucular.tech", 1251, serial);

            lock (nodes)
            {
                nodes.Add(this);
            }

            LC_Interface.SetAddressCallback(addressChanges);
        }

        public void Dispose()
        {
            if (descriptor != IntPtr.Zero)
            {
                lock (nodes)
                {
                    nodes.Remove(this);
                }
                if (objects_node != null)
                {
                    Marshal.FreeHGlobal((IntPtr)objects_node);
                    objects_node = null;
                }
                LC_Node_Destroy(descriptor);
            }
            GC.SuppressFinalize(this);
        }

        public void SetIdentity(string nodeName, string deviceName, string vendorName, ushort codePage, uint[]? serial = null)
        {
            uint[] s = serial ?? new uint[] { 1, 2, 3, 4 };
            LC_Node_SetIdentity(descriptor, nodeName, deviceName, vendorName, codePage, s);
        }

        public void SetDirectories(IntPtr directories, ushort count)
        {
            LC_Node_SetDirectories(descriptor, directories, count);
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

            VirtualCanBus.ActiveSenderNode = this;
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
            VirtualCanBus.ActiveSenderNode = this;
            while (true)
            {
                lib_networkManager(DescriptorPtr, 1);
                Thread.Sleep(1);
            }
        }

        void nodeReceive()
        {
            VirtualCanBus.ActiveSenderNode = this;
            while (true)
            {
                lib_receiveManager(DescriptorPtr);
                Thread.Sleep(1);
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
                    if (objects[i] == null)
                    {
                        objects_node[i].Index = 0;
                        objects_node[i].Address = null;
                        continue;
                    }
                    objects_node[i].Attributes = objects[i].Attributes;
                    if (objects[i].Index > (ushort)LC_SystemMessage.MaxMessageID)
                        throw new IndexOutOfRangeException("Index " + objects[i].Index.ToString() + " is out of range!");

                    objects_node[i].Index = objects[i].Index;
                    objects_node[i].Size = objects[i].Size;
                    objects_node[i].Address = (void*)objects[i].Pointer;

                }
                //assign obj list
                LC_Node_SetObjects(descriptor, (IntPtr)objects_node, size);

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
        public LC_Return SendRequest(ushort target, ushort index, bool TCP = false)
        {
            if (target > (ushort)LC_Address.Broadcast)
                throw new ArgumentOutOfRangeException("Target ID out of range!");

            VirtualCanBus.ActiveSenderNode = this;
            return lib_sendRequestSpec(DescriptorPtr, target, index, 0, (byte)(TCP ? 1 : 0));
        }

        public LC_Return SendData(byte[] bytes, byte target, ushort index, bool TCP = false)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            lc_objectRecord message = new lc_objectRecord();
            message.NodeID = target;
            message.Size = bytes.Length;

            if (bytes.Length > 0)
            {
                message.Attributes = (ushort)lc_objectAttributes_internal.Cleanup;
                if (TCP)
                    message.Attributes |= (ushort)lc_objectAttributes_internal.TCP;

                message.Address = (void*)LC_Malloc((IntPtr)bytes.Length);
                if (message.Address == null)
                    return LC_Return.MallocFail;
                Marshal.Copy(bytes, 0, (IntPtr)message.Address, bytes.Length);
            }
            else
            {
                if (TCP)
                    message.Attributes = (ushort)lc_objectAttributes_internal.TCP;
                message.Address = null;
            }

            VirtualCanBus.ActiveSenderNode = this;
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

        internal static LC_Node GetNodeByDesc(IntPtr desc)
        {
            lock (nodes)
            {
                foreach (var node in nodes)
                {
                    if (node.descriptor == desc)
                        return node;
                }
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
