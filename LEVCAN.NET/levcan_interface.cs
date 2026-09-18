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
        [DllImport("LEVCANlib", EntryPoint = "LC_ReceiveHandler", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern void lib_ReceiveHandler(IntPtr node, uint header, [MarshalAs(UnmanagedType.LPArray, SizeConst = 2)] uint[] data, byte length);

        [DllImport("LEVCANlib", EntryPoint = "LC_ReceiveHandler", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern void lib_ReceiveHandler(IntPtr node, uint header, [MarshalAs(UnmanagedType.LPArray, SizeConst = 8)] byte[] data, byte length);

        [DllImport("LEVCANlib", EntryPoint = "LC_Set_SendCallback", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern void lib_setSendCallback(SendCallback callback);

        [DllImport("LEVCANlib", EntryPoint = "LC_Set_FilterCallback", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern void lib_setFilterCallback(_filterCallback callback);

        [DllImport("LEVCANlib", EntryPoint = "LC_ConfigureFilters", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern void lib_ConfigureFilters(IntPtr node);

        [DllImport("LEVCANlib", EntryPoint = "LC_Set_AddressCallback", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern void _setAddressCallback(_remoteNodeCallback callback);

        static uint[] reg;
        static uint[] mask;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate LC_Return SendCallback(uint header, [MarshalAs(UnmanagedType.LPArray, SizeConst = 2)] uint[] data, byte length);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate LC_Return FilterCallback(uint reg, uint mask, byte index);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate LC_Return _filterCallback(uint* reg, uint* mask, byte cnt);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate void _remoteNodeCallback(LC_NodeShortName shortname, ushort index, ushort state);
        public delegate void RemoteNodeCallback(LC_NodeShortName shortname, ushort index, LC_AddressState state);

        //anti-garbage collector
        private static SendCallback send_callback;
        private static FilterCallback filter_callback;
        private static _filterCallback filter_callback_private;
        private static _remoteNodeCallback _addressCallback;
        private static RemoteNodeCallback addressCallback;

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

        // Queues are now handled natively inside LEVCANlib. Retained for API compatibility.
        public static void InitQHandlers()
        {
        }

        public static bool IsReady()
        {
            return send_callback != null && filter_callback != null && filter_callback_private != null;
        }

        public static void SetAddressCallback(RemoteNodeCallback callback)
        {
            addressCallback = callback;//public call
            _addressCallback = remoteCallback; //private call
            _setAddressCallback(_addressCallback);
        }
        public static void remoteCallback(LC_NodeShortName shortname, ushort index, ushort state)
        {
            try
            {
                //non blocking callback
                addressCallback?.Invoke(shortname, index, (LC_AddressState)state);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LC_Interface.remoteCallback exception: {ex.Message}");
            }
        }

        public static void ConfigureFilters(LC_Node node)
        {
            lib_ConfigureFilters(node.DescriptorPtr);
        }
    }
}
