using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LEVCAN
{
    public enum LC_Return
    {
        Ok, DataError, ObjectError, BufferFull, BufferEmpty, NodeOffline, MallocFail, Collision, Timeout, OutOfRange, AccessError
    };

    public enum LC_Address
    {
        Preffered = 0, Normal = 64, Null = 126, Broadcast = 127,
    };

    public enum LC_AddressState
    {
        Nothing,
        New,
        Changed,
        Deleted
    };

    public enum LC_SystemMessage
    {
        AddressClaimed = 0x380,
        ComandedAddress,
        NodeName = 0x388,
        DeviceName,
        VendorName,
        VendorCode,
        HWVersion,
        SWVersion,
        SerialNumber,
        Parameters,
        Variables,
        Events,
        Trace,
        DateTime,
        SWUpdate,
        Shutdown,
        FileServer,
        FileClient,
        SaveData,
        End,
        MaxMessageID = 1023
    };

    public enum LC_NodeState
    {
        Disabled, NetworkDiscovery, WaitingClaim, Online
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LC_NodeShortName
    {
        public ulong Value;
        public ushort NodeID;

        /*			uint32_t Configurable :1;
			uint32_t Variables :1;
			uint32_t SWUpdates :1;
			uint32_t Events :1;
			uint32_t FileServer :1;
			uint32_t reserved :(64 - 6 - 32);
			uint32_t DynamicID :1;
			uint32_t DeviceType :10;
			uint32_t ManufacturerCode :10;
			uint32_t SerialNumber :12;*/
        public LC_NodeShortName(ushort nodeid)
        {
            Value = 0;
            NodeID = nodeid;
        }

        public bool Configurable
        {
            get { return (Value & (1ul << 0)) > 0; }
            set
            {
                if (value)
                    Value |= (1ul << 0);
                else
                    Value &= ~(1ul << 0);
            }
        }
        public bool Variables
        {
            get { return (Value & (1ul << 1)) > 0; }
            set
            {
                if (value)
                    Value |= (1ul << 1);
                else
                    Value &= ~(1ul << 1);
            }
        }
        public bool SWUpdates
        {
            get { return (Value & (1ul << 2)) > 0; }
            set
            {
                if (value)
                    Value |= (1ul << 2);
                else
                    Value &= ~(1ul << 2);
            }
        }
        public bool Events
        {
            get { return (Value & (1ul << 3)) > 0; }
            set
            {
                if (value)
                    Value |= (1ul << 3);
                else
                    Value &= ~(1ul << 3);
            }
        }
        public bool FileServer
        {
            get { return (Value & (1ul << 4)) > 0; }
            set
            {
                if (value)
                    Value |= (1ul << 4);
                else
                    Value &= ~(1ul << 4);
            }
        }

        public Encoding CodePage
        {
            get { return Encoding.GetEncoding((int)(Value >> 5) & 0xFFFF); }
            set
            {
                Value &= ~(0xFFFFul << 5);
                Value |= (ulong)(value.CodePage & 0xFFFF) << 5;
            }
        }

        public bool DynamicID
        {
            get { return (Value & (1ul << 31)) > 0; }
            set
            {
                if (value)
                    Value |= (1ul << 31);
                else
                    Value &= ~(1ul << 31);
            }
        }

        public ushort DeviceType
        {
            get { return (ushort)((Value >> 32) & 0x3FFul); }
            set
            {
                Value &= ~(0x3FFul << 32);
                Value |= ((value & 0x3FFul) << 32);
            }
        }
        public ushort ManufacturerCode
        {
            get { return (ushort)((Value >> 42) & 0x3FFul); }
            set
            {
                Value &= ~(0x3FFul << 42);
                Value |= ((value & 0x3FFul) << 42);
            }
        }
        public ushort SerialNumber
        {
            get { return (ushort)((Value >> 52) & 0xFFFul); }
            set
            {
                Value &= ~(0xFFFul << 52);
                Value |= ((value & 0xFFFul) << 52);
            }
        }
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LC_HeaderPacked
    {
        private BitVector32 buffer;

        private static BitVector32.Section _Source;
        private static BitVector32.Section _Target;
        private static BitVector32.Section _MsgID;
        private static BitVector32.Section _EoM;
        private static BitVector32.Section _Parity;
        private static BitVector32.Section _RTS_CTS;
        private static BitVector32.Section _Priority;
        private static BitVector32.Section _Request;

        public LC_HeaderPacked(uint id)
        {
            // allocate the bitfield
            buffer = new BitVector32((int)id);
            // initialize bitfield sections
            _Source = BitVector32.CreateSection(0x7F);
            _Target = BitVector32.CreateSection(0x7F, _Source);
            _MsgID = BitVector32.CreateSection(0x3FF, _Target);
            _EoM = BitVector32.CreateSection(0x01, _MsgID);
            _Parity = BitVector32.CreateSection(0x01, _EoM);
            _RTS_CTS = BitVector32.CreateSection(0x01, _Parity);
            _Priority = BitVector32.CreateSection(0x3, _RTS_CTS);
            _Request = BitVector32.CreateSection(0x1, _Priority);

        }

        public byte Source
        {
            get { return (byte)buffer[_Source]; }
            set { buffer[_Source] = value; }
        }
        public byte Target
        {
            get { return (byte)buffer[_Target]; }
            set { buffer[_Target] = value; }
        }
        public ushort MsgID
        {
            get { return (ushort)buffer[_MsgID]; }
            set { buffer[_MsgID] = value; }
        }
        public byte EoM
        {
            get { return (byte)buffer[_EoM]; }
            set { buffer[_EoM] = value; }
        }
        public byte Parity
        {
            get { return (byte)buffer[_Parity]; }
            set { buffer[_Parity] = value; }
        }
        public byte RTS_CTS
        {
            get { return (byte)buffer[_RTS_CTS]; }
            set { buffer[_RTS_CTS] = value; }
        }

        public byte Priority
        {
            get { return (byte)buffer[_Priority]; }
            set { buffer[_Priority] = value; }
        }

        public byte Request
        {
            get { return (byte)buffer[_Request]; }
            set { buffer[_Request] = value; }
        }

        public override string ToString()
        {
            return "SRC:" + this.Source.ToString() + " DST:" + Target.ToString() + " ID:" + MsgID.ToString() + (Request == 1 ? " R" : " d") + (Parity == 1 ? " P" : " _") + (RTS_CTS == 1 ? " RC" : " __");
        }
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LC_Header
    {
        public byte Source;
        public byte Target;
        public ushort MsgID;
        ushort controlBits;

        public LC_Header(byte from, byte to, ushort msgID)
        {
            Source = from;
            Target = to;
            controlBits = 0;
            MsgID = msgID;
        }

        public LC_Header(LC_HeaderPacked packed)
        {
            this = new LC_Header(packed.Source, packed.Target, packed.MsgID);
            EoM = packed.EoM > 0;
            Parity = packed.Parity > 0;
            RTS_CTS = packed.RTS_CTS > 0;
            Priority = packed.Priority;
            Request = packed.Request > 0;
        }

        public bool EoM
        {
            get { return (controlBits & (1u << 0)) > 0; }
            set
            {
                if (value)
                    controlBits |= (ushort)(1u << 0);
                else
                {
                    unchecked { controlBits &= (ushort)~(1u << 0); }
                }
            }
        }
        public bool Parity
        {
            get { return (controlBits & (1u << 1)) > 0; }
            set
            {
                if (value)
                    controlBits |= (ushort)(1u << 1);
                else
                {
                    unchecked { controlBits &= (ushort)~(1u << 1); }
                }
            }
        }
        public bool RTS_CTS
        {
            get { return (controlBits & (1u << 2)) > 0; }
            set
            {
                if (value)
                    controlBits |= (ushort)(1u << 2);
                else
                {
                    unchecked { controlBits &= (ushort)~(1u << 2); }
                }
            }
        }
        public byte Priority
        {
            get { return (byte)((controlBits >> 3) & 0x3); }
            set
            {
                value &= 0x3;
                unchecked { controlBits &= (ushort)~(3u << 2); }
                controlBits |= (ushort)(value << 3);
            }
        }
        public bool Request
        {
            get { return (controlBits & (1u << 5)) > 0; }
            set
            {
                if (value)
                    controlBits |= (ushort)(1u << 5);
                else
                {
                    unchecked { controlBits &= (ushort)~(1u << 5); }
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    internal unsafe struct lc_object_intenal
    {
        public ushort Index; //message id
        public ushort Attributes; /*LC_ObjectAttributes_t*/
        public int Size; //in bytes, can be negative (useful for strings), i.e. -1 = maximum length 1, -10 = maximum length 10. Request size 0 returns any first object
        public void* Address; //pointer to variable or LC_FunctionCall_t or LC_ObjectRecord_t[]. if LC_ObjectAttributes_t.Pointer=1, this is pointer to pointer
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    internal unsafe struct lc_objectRecord_intenal
    {
        public byte NodeID; //message id
        public ushort/*LC_ObjectAttributes_t*/ Attributes;
        public int Size; //in bytes, can be negative (useful for strings), i.e. -1 = maximum length 1, -10 = maximum length 10. Request size 0 returns any first object
        void* address; //pointer to variable or LC_FunctionCall_t or LC_ObjectRecord_t[]. if LC_ObjectAttributes_t.Pointer=1, this is pointer to pointer
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
    public unsafe struct LC_NodeDescriptor
    {
        void* driver;
        private byte* _NodeName;
        private byte* _DeviceName;
        private byte* _VendorName;
        internal lc_object_intenal* Objects;
        internal void* Directories;
        public LC_NodeShortName ShortName;
        //[MarshalAs(UnmanagedType.LPArray, SizeConst = 4)]
        public uint Serial1;
        public uint Serial2;
        public uint Serial3;
        public uint Serial4;
        public uint LastTXtime;
        internal ushort ObjectsSize;
        internal ushort SystemSize;
        public ushort DirectoriesSize;
        public ushort LastID;
        internal byte state;
        public byte AccessLevel;

        public string NodeName
        {
            get { return Text8z.PtrToString((IntPtr)_NodeName, ShortName.CodePage); }
            set
            {
                if ((IntPtr)_NodeName != IntPtr.Zero)
                    Marshal.FreeHGlobal((IntPtr)_NodeName);
                _NodeName = (byte*)Text8z.StringToPtr(value, ShortName.CodePage);
            }
        }
        public string DeviceName
        {
            get { return Text8z.PtrToString((IntPtr)_DeviceName, ShortName.CodePage); }
            set
            {
                if ((IntPtr)_DeviceName != IntPtr.Zero)
                    Marshal.FreeHGlobal((IntPtr)_DeviceName);
                _DeviceName = (byte*)Text8z.StringToPtr(value, ShortName.CodePage);
            }
        }
        public string VendorName
        {
            get { return Text8z.PtrToString((IntPtr)_VendorName, ShortName.CodePage); }
            set
            {
                if ((IntPtr)_VendorName != IntPtr.Zero)
                    Marshal.FreeHGlobal((IntPtr)_VendorName);
                _VendorName = (byte*)Text8z.StringToPtr(value, ShortName.CodePage);
            }
        }
        public LC_NodeState State { get { return (LC_NodeState)state; } }
    };

}
