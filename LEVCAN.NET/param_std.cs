﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LEVCAN
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    internal unsafe struct lcpc_Entry_t
    {
        void* _variable; //address of variable or structure
        void* _descriptor; //depends on LCP_Type_t
        byte* _name; //null terminated
        byte* _textData; //null terminated
        public ushort EntryIndex;
        public ushort VarSize; //in bytes  // uint16_t DirectoryIndex;
        public ushort DescSize; //in bytes
        public ushort TextSize; //for checking
        public byte EntryType; //LCP_Type_t
        public byte Mode; //LCP_Mode_t
        public IntPtr Variable { get { return (IntPtr)_variable; } }
        public IntPtr Descriptor { get { return (IntPtr)_descriptor; } }
        public IntPtr Name { get { return (IntPtr)_name; } }
        public IntPtr TextData { get { return (IntPtr)_textData; } }
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    internal unsafe struct lcpc_Directory_t
    {
        public byte* _name; //null terminated
        public ushort Size;
        public ushort DirectoryIndex;
        public IntPtr Name { get { return (IntPtr)_name; } }
    };

    [Flags]
    public enum LCP_Mode
    {
        Normal = 0,             //RW
        ReadOnly = 1,           //
        WriteOnly = 2,          //
        Invalid = 3,            //
        LiveUpdate = 1 << 2,    //update values realtime
        LiveChange = 1 << 3,    //apply value changes realtime
        Empty = 0xFF
    };

    public enum LCP_EntryType
    {
        Folder, Label, //null Descriptor
        Bool, //LCP_Bool_t
        Enum, //LCP_Enum_t
        Bitfield32, //LCP_Bitfield_t
        Int32, //LCP_int
        Uint32, //LCP_uint
        Int64, //LCP_long
        Uint64, //LCP_ulong
        Float, //LCP_Float_t
        Double, //LCP_Double_t
        Decimal32, //LCP_Decimal32_t
        String, //LCP_String_t
        End = 0xFF
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LCP_Enum
    {
        public uint Min;
        public uint Size;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LCP_Bitfield32
    {
        public uint Mask;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LCP_Uint32
    {
        public uint Min;
        public uint Max;
        public uint Step;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LCP_Int32
    {
        public int Min;
        public int Max;
        public int Step;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LCP_Uint64
    {
        public ulong Min;
        public ulong Max;
        public ulong Step;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LCP_Int64
    {
        public long Min;
        public long Max;
        public long Step;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LCP_Float
    {
        public float Min;
        public float Max;
        public float Step;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LCP_Double
    {
        public double Min;
        public double Max;
        public double Step;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LCP_Decimal32
    {
        public int Min;
        public int Max;
        public int Step;
        public byte Decimals;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LCP_String
    {
        ushort flags; //LCP_StringFlags
        public ushort MinLength;
        public ushort MaxLength;
        public ushort Reserved;

        public LCP_StringFlags Flags
        { get { return (LCP_StringFlags)flags; } }
    };

    [Flags]
    public enum LCP_StringFlags
    {
        None = 0, // just text
        Numeric = 1 << 0, // numbers only
        Hexadecimal = 1 << 1, // Hex numbers
        Letters = 1 << 2, // letters only
        NoBlank = 1 << 3, // no blank letters
        SpecChar = 1 << 4, // special character, e.g., ! @ # ?
        Password = 1 << 5, // show ***
        Email = 1 << 6, // one@two.com
        Phone = 1 << 7, // +1(234)567 89 89
        Website = 1 << 8, // www.dot.com
        Multiline = 1 << 9, // allow /r/n
        Uppercase = 1 << 10, // UPPERCASE
        UTF8 = 1 << 14, // force UTF8 encoding (char)
        UTF16 = 1 << 15, // force UTF16 encoding (wchar)
        ASCII = UTF8 | UTF16 // force ASCII encoding (char)
    };
}
