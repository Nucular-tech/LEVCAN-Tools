using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LEVCAN
{
    public unsafe class LC_Interface
    {
        [DllImport("LEVCANlib", EntryPoint = "LC_ReceiveHandler", CharSet = CharSet.Ansi)]
        public static extern void lib_ReceiveHandler(IntPtr node, uint header, [MarshalAs(UnmanagedType.LPArray, SizeConst = 2)] uint[] data, byte length);

        [DllImport("LEVCANlib", EntryPoint = "LC_ReceiveHandler", CharSet = CharSet.Ansi)]
        public static extern void lib_ReceiveHandler(IntPtr node, uint header, [MarshalAs(UnmanagedType.LPArray, SizeConst = 8)] byte[] data, byte length);

        [DllImport("LEVCANlib", EntryPoint = "LC_Set_SendCallback", CharSet = CharSet.Ansi)]
        private static extern void lib_setSendCallback(SendCallback callback);

        [DllImport("LEVCANlib", EntryPoint = "LC_Set_FilterCallback", CharSet = CharSet.Ansi)]
        private static extern void lib_setFilterCallback(_filterCallback callback);

        [DllImport("LEVCANlib", EntryPoint = "LC_ConfigureFilters", CharSet = CharSet.Ansi)]
        private static extern void lib_ConfigureFilters(IntPtr node);

        [DllImport("LEVCANlib", EntryPoint = "LC_Set_QueueCallbacks", CharSet = CharSet.Ansi)]
        private static extern void _setQueueCallbacks(qCreate create, qDelete delete, qReceive receive, qSendBack toback);

        [DllImport("LEVCANlib", EntryPoint = "LC_Set_AddressCallback", CharSet = CharSet.Ansi)]
        private static extern void _setAddressCallback(_remoteNodeCallback callback);

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
            if (reg != null && filter_callback != null)
            {
                for (byte i = 0; i < reg.Length; i++)
                {
                    filter_callback(reg[i], mask[i], i);
                }
            }
        }

        struct lcQueue_t
        {
            public uint QueueIndex;
            public uint Deleted;
        }

        //little queue wrapper for low-level code. 
        //since levcan only creates queues we store them in a list and get acces by index
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
}
