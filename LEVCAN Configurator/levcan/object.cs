using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace LEVCAN
{
    [Flags]
    public enum LC_ObjectAttributes
    {
        Readable = 1 << 0,  //Nodes can read from the variable
        Writable = 1 << 1,  //Nodes may write to the variable
        TCP = 1 << 2,       //Node should control packets sent (RTS/CTS) and create special tx/rx buffer
        PriorityLow = 0 << 3, //2b
        PriorityMid = 1 << 3,
        PriorityControl = 2 << 3,
        PriorityHigh = 3 << 3,
    }

    [Flags]
    enum lc_objectAttributes_internal
    {
        Readable = 1 << 0,  //Nodes can read from the variable
        Writable = 1 << 1,  //Nodes may write to the variable
        TCP = 1 << 2,       //Node should control packets sent (RTS/CTS) and create special tx/rx buffer
        PriorityLow = 0 << 3, //2b
        PriorityMid = 1 << 3,
        PriorityControl = 2 << 3,
        PriorityHigh = 3 << 3,
        Record = 1 << 5,     //Object remapped to record array LC_ObjectRecord_t[Size],were LC_Object_t.Size will define array size
        Function = 1 << 6,   //Functional call LC_FunctionCall_t, memory pointer will be cleared after call
                             //received data will be saved as pointer to memory area, if there is already exists, it will be free
        Pointer = 1 << 7,    //TX - data taken from pointer (where Address is pointer to pointer)
        Cleanup = 1 << 8,	//after transmission pointer will call memfree
        Queue = 1 << 9,	    //will place data on specified queue (address should have queue pointer)
    }


    public class LC_Object : LC_IObject
    {
        public ushort Index { get; }
        lc_objectAttributes_internal attributes;
        public object Obj;
        public int Size { get; }

        GCHandleProvider gch;

        /// <summary>
        /// Create object, struct type
        /// </summary>
        /// <param name="index">Message index</param>
        /// <param name="target">Structure with data</param>
        /// <param name="attr">Attributes of an object</param>
        public LC_Object(ushort index, object target, LC_ObjectAttributes attr)
        {
            Index = index;
            Obj = target;
            Size = Marshal.SizeOf(target.GetType());

            attributes = (lc_objectAttributes_internal)attr;
            attributes &= ~(lc_objectAttributes_internal.Pointer | lc_objectAttributes_internal.Record
                | lc_objectAttributes_internal.Function | lc_objectAttributes_internal.Cleanup);
            gch = new GCHandleProvider(Obj);
        }

        public IntPtr Pointer { get { return gch.Pointer; } }

        public ushort Attributes { get { return (ushort)attributes; } }

    }

    public unsafe class LC_ObjectFunction : LC_IObject
    {
        public delegate void Callback(LC_Header header, object data);
        private delegate void lc_callback(LC_NodeDescriptor* descriptor, LC_Header header, void* data, int size);

        private Callback _callback;
        private Delegate _callback_api;
        lc_objectAttributes_internal attributes;

        public ushort Index { get; }
        public int Size { get; }
        public IntPtr Pointer { get; }

        Type type;

        /// <summary>
        /// Create callback object, struct type
        /// </summary>
        /// <param name="index">Message index</param>
        /// <param name="callback">Function called, when data received.</param>
        /// <param name="attr">Attributes of an object</param>
        /// <param name="t">Structure type</param>
        public LC_ObjectFunction(ushort index, Callback callback, LC_ObjectAttributes attr, Type t)
        {
            type = t;
            Index = index;
            Size = Marshal.SizeOf(t);
            attributes = (lc_objectAttributes_internal)attr;
            attributes &= ~(lc_objectAttributes_internal.Pointer | lc_objectAttributes_internal.Record | lc_objectAttributes_internal.Cleanup);
            attributes |= lc_objectAttributes_internal.Function;

            _callback = callback;
            _callback_api = new lc_callback(lc_Callback);
            Pointer = Marshal.GetFunctionPointerForDelegate(_callback_api);
        }

        /// <summary>
        /// Create callback object, data type
        /// </summary>
        /// <param name="index">Message index</param>
        /// <param name="callback">Function called, when data received.</param>
        /// <param name="attr">Attributes of an object</param>
        /// <param name="size">Data size to be received. Negative numbers limits max size</param>
        public LC_ObjectFunction(ushort index, Callback callback, LC_ObjectAttributes attr, short size)
        {
            type = null;
            Index = index;
            Size = size;
            attributes = (lc_objectAttributes_internal)attr;
            attributes &= ~(lc_objectAttributes_internal.Pointer | lc_objectAttributes_internal.Record | lc_objectAttributes_internal.Cleanup);
            attributes |= lc_objectAttributes_internal.Function;

            _callback = callback;
            _callback_api = new lc_callback(lc_Callback);
            Pointer = Marshal.GetFunctionPointerForDelegate(_callback_api);
        }

        public ushort Attributes { get { return (ushort)attributes; } }

        //overlay from LEVCAN unsafe to safe managed code
        internal void lc_Callback(LC_NodeDescriptor* descriptor, LC_Header header, void* data, int size)
        {
            if (type == null)
            {
                byte[] dataBytes = new byte[size];
                //levcan.c will take care about memfree
                Marshal.Copy((IntPtr)data, dataBytes, 0, 0);
                _callback.Invoke(header, dataBytes);
            }
            else
            {
                var converted = Marshal.PtrToStructure((IntPtr)data, type);
                _callback.Invoke(header, converted);
            }
        }
    }

    public unsafe class LC_ObjectString : LC_IObject
    {
        public delegate void Callback(LC_Header header, string text);
        private delegate void lc_callback(LC_NodeDescriptor* descriptor, LC_Header header, void* data, int size);

        public Callback OnChange;
        //gc anticollector
        private Delegate _callback_api;
        string textStr;
        lc_objectAttributes_internal attributes;

        public LC_ObjectString(ushort index, string text, ushort maxSize, LC_ObjectAttributes attr)
        {
            Index = index;
            Size = -((int)maxSize);
            textStr = text;

            attributes = (lc_objectAttributes_internal)attr | lc_objectAttributes_internal.Function;
            attributes &= ~(lc_objectAttributes_internal.Pointer | lc_objectAttributes_internal.Record | lc_objectAttributes_internal.Cleanup);

            _callback_api = new lc_callback(lc_Callback);
            Pointer = Marshal.GetFunctionPointerForDelegate(_callback_api);
        }
        //overlay from LEVCAN unsafe to safe managed code
        internal void lc_Callback(LC_NodeDescriptor* descriptor, LC_Header header, void* data, int size)
        {
            var owner = LC_Node.GetNodeByDesc(descriptor);
            if (header.Request)
            {
                //request to send
            }
            else
            {
                var sname = owner.GetNodeShortName(header.Source);
                Encoding page;
                if (sname.NodeID == (ushort)LC_Address.Broadcast)
                    page = descriptor->ShortName.CodePage;//not found, use own codepage
                else
                    page = sname.CodePage; //use sender codepage to decode
                //received data
                textStr = Text8z.PtrToString((IntPtr)data, page, size);

                if (OnChange != null)
                    OnChange.Invoke(header, textStr);
            }
        }

        public ushort Index { get; }
        public int Size { get; }
        public IntPtr Pointer { get; }
        public ushort Attributes { get { return (ushort)attributes; } }
    }


    /*public class LC_ObjectRecord : ILC_Object
    {
        public byte NodeID;
        lc_objectAttributes_internal attributes;
        public object Obj;
        public int Size { get; }

        GCHandleProvider gch;

        public LC_ObjectRecord(byte nodeID, object target, LC_ObjectAttributes attr)
        {
            NodeID = nodeID;
            Obj = target;
            Size = Marshal.SizeOf(target.GetType());
            attributes = (lc_objectAttributes_internal)attr;
            gch = new GCHandleProvider(Obj);
        }

        public IntPtr Pointer { get { return gch.Pointer; } }

        public ushort Attributes { get { return (ushort)attributes; } }

        public bool Record { get { return true; } }
    }*/

    internal interface LC_IObject
    {
        ushort Index { get; }
        ushort Attributes { get; }
        int Size { get; }
        IntPtr Pointer { get; }
    }

}
