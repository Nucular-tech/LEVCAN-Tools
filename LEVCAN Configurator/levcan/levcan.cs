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

namespace LEVCAN
{
    unsafe class LC_Interface
    {
        static uint[] reg;
        static uint[] mask;

        public delegate LC_Return SendCallback(uint header, [MarshalAs(UnmanagedType.LPArray, SizeConst = 2)] uint[] data, byte length);
        public delegate LC_Return FilterCallback(uint reg, uint mask, byte index);
        delegate LC_Return _filterCallback(uint* reg, uint* mask, byte cnt);

        delegate void _remoteNodeCallback(LC_NodeShortName shortname, ushort index, ushort state);
        public delegate void RemoteNodeCallback(LC_NodeShortName shortname, ushort index, LC_AddressState state);

        //anti-garbage collector
        private static SendCallback send_callback;
        private static FilterCallback filter_callback;
        private static _filterCallback filter_callback_private;
        private static _remoteNodeCallback _addressCallback;
        private static RemoteNodeCallback addressCallback;
        private static bool queuesSet = false;

        [DllImport("LEVCANlib", EntryPoint = "LC_ReceiveHandler", CharSet = CharSet.Ansi)]
        public static extern void lib_ReceiveHandler(IntPtr node, uint header, [MarshalAs(UnmanagedType.LPArray, SizeConst = 2)] uint[] data, byte length);

        [DllImport("LEVCANlib", EntryPoint = "LC_Set_SendCallback", CharSet = CharSet.Ansi)]
        private static extern void lib_setSendCallback(SendCallback callback);

        [DllImport("LEVCANlib", EntryPoint = "LC_Set_FilterCallback", CharSet = CharSet.Ansi)]
        private static extern void lib_setFilterCallback(_filterCallback callback);

        [DllImport("LEVCANlib", EntryPoint = "LC_ConfigureFilters", CharSet = CharSet.Ansi)]
        private static extern void lib_ConfigureFilters(IntPtr node);

        public static void SetFilterCallback(FilterCallback callback)
        {
            filter_callback = callback;
            filter_callback_private = filterCallback;
            lib_setFilterCallback(filter_callback_private);
        }

        public static void SetSendCallback(SendCallback callback)
        {
            send_callback = callback;
            lib_setSendCallback(send_callback);
        }


        private static LC_Return filterCallback(uint* regv, uint* maskv, byte cnt)
        {
            //store filter data
            if (cnt < 3)
            {
                reg = new uint[cnt];
                mask = new uint[cnt];
                for (int i = 0; i < cnt; i++)
                {
                    reg[i] = regv[i];
                    mask[i] = maskv[i];
                }
            }

            InitFilters();
            return LC_Return.Ok;
        }
        //Init filter any time interface is ready
        public static void InitFilters()
        {
            if (reg != null)
            {
                for (byte i = 0; i < reg.Length; i++)
                {
                    filter_callback(reg[i], mask[i], i);
                }
            }
        }

        delegate IntPtr qCreate(int length, uint itemsize);
        delegate void qDelete(lcQueue_t* queue);
        delegate int qReceive(lcQueue_t* queue, byte* buffer, int timeToWait);
        delegate int qSendBack(lcQueue_t* queue, byte* buffer, int timeToWait);
        //anti-garbage collector
        private static qCreate qcreate;
        private static qDelete qdelete;
        private static qReceive qreceive;
        private static qSendBack qsendback;
        static List<BCollectionSized> qlist;


        [DllImport("LEVCANlib", EntryPoint = "LC_Set_QueueCallbacks", CharSet = CharSet.Ansi)]
        private static extern void _setQueueCallbacks(qCreate create, qDelete delete, qReceive receive, qSendBack toback);

        struct lcQueue_t
        {
            public uint QueueIndex;
            public uint Deleted;
        }
        //little queue wrapper for low-level code. 
        //since levcan only creates queues we store them in a list and get acces by index
        //because 
        public static void InitQHandlers()
        {
            if (qlist == null)
            {
                qlist = new List<BCollectionSized>();
                qcreate = QueueCreate;
                qdelete = QueueDelete;
                qreceive = QueueReceive;
                qsendback = QueueSendBack;
                _setQueueCallbacks(qcreate, qdelete, qreceive, qsendback);
                queuesSet = true;
            }
        }

        private static int QueueSendBack(lcQueue_t* queue, byte* buffer, int timeToWait)
        {
            if (queue == null)
                return 0;

            if (queue->Deleted == 0 && queue->QueueIndex < qlist.Count)
            {
                var specificQ = qlist[(int)queue->QueueIndex];
                byte[] newdata = new byte[specificQ.ItemSize];
                //copy data to receiver
                for (int i = 0; i < newdata.Length; i++)
                {
                    if (buffer != null)
                        newdata[i] = buffer[i];
                    else
                        newdata[i] = 0;
                }
                bool added = specificQ.TryAdd(newdata, timeToWait);
                return added ? 1 : 0;
            }
            return 0;
        }

        private static int QueueReceive(lcQueue_t* queue, byte* buffer, int timeToWait)
        {
            if (queue->Deleted == 0 && queue->QueueIndex < qlist.Count)
            {
                var specificQ = qlist[(int)queue->QueueIndex];
                byte[] bytes;
                bool taken = specificQ.TryTake(out bytes, (int)timeToWait);
                if (taken && buffer != null)
                {
                    //copy data to receiver
                    for (int i = 0; i < specificQ.ItemSize && i < bytes.Length; i++)
                    {
                        buffer[i] = bytes[i];
                    }
                    return 1;
                }
            }
            return 0;
        }

        private static IntPtr QueueCreate(int length, uint itemsize)
        {
            //levcan does not delete much queues, and usually stores them. so here is simple q-list
            BCollectionSized collect = new BCollectionSized(length, itemsize);
            qlist.Add(collect);
            lcQueue_t qi = new lcQueue_t();
            qi.Deleted = 0;
            qi.QueueIndex = (uint)qlist.IndexOf(collect);
            return qi.ToIntPtr();
        }

        private static void QueueDelete(lcQueue_t* queue)
        {
            if (queue->Deleted == 0 && queue->QueueIndex < qlist.Count)
            {
                qlist[(int)queue->QueueIndex].Dispose();
                queue->Deleted = 1;
            }
        }

        public static bool IsReady()
        {
            return send_callback != null && filter_callback != null && filter_callback_private != null && queuesSet;
        }


        [DllImport("LEVCANlib", EntryPoint = "LC_Set_AddressCallback", CharSet = CharSet.Ansi)]
        private static extern void _setAddressCallback(_remoteNodeCallback callback);

        public static void SetAddressCallback(RemoteNodeCallback callback)
        {
            addressCallback = callback;//public call
            _addressCallback = remoteCallback; //private call
            _setAddressCallback(_addressCallback);
        }
        public static void remoteCallback(LC_NodeShortName shortname, ushort index, ushort state)
        {
            //non blocking callback
            addressCallback(shortname, index, (LC_AddressState)state);
        }

        public static void ConfigureFilters(LC_Node node)
        {
            lib_ConfigureFilters(node.DescriptorPtr);
        }
    }

    internal class BCollectionSized : BlockingCollection<byte[]>
    {
        public uint ItemSize;
        public BCollectionSized(int length, uint itemsize) : base((int)itemsize)
        {
            ItemSize = itemsize;
        }
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

        lc_object* objects_node;
        LC_IObject[] objects;

        public LC_Node(byte nodeID)
        {
            var location = Assembly.GetExecutingAssembly().Location;
            var assemblyDir = Path.GetDirectoryName(location);
            string arch = Environment.Is64BitProcess ? "-x64" : "-x86";
            string fullPath;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                fullPath = Path.Combine(assemblyDir, "osx" + arch);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                fullPath = Path.Combine(assemblyDir, "linux" + arch);
            }
            else // RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            {
                fullPath = Path.Combine(assemblyDir, "win" + arch);
            }
            string backup = Environment.CurrentDirectory;
            //setup load path, import DLL, reset path
            Environment.CurrentDirectory = fullPath;
            descriptor = (LC_NodeDescriptor*)LC_LibInit();
            Environment.CurrentDirectory = backup;

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

        internal LC_IObject[] Objects
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
    }
    public interface IObjectHandler
    {
        object Value { get; set; }
    }
}
