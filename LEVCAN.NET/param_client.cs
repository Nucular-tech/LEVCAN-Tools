using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LEVCAN
{
    public class LC_ParamClient
    {
        [DllImport("LEVCANlib", EntryPoint = "LCP_RequestEntry", CharSet = CharSet.Ansi)]
        private static extern LC_Return lib_requestEntry(IntPtr mynode, byte from_node, ushort directory_index, ushort entry_index, ref lcpc_Entry_t out_entry);

        [DllImport("LEVCANlib", EntryPoint = "LCP_CleanEntry", CharSet = CharSet.Ansi)]
        private static extern void lib_CleanEntry(ref lcpc_Entry_t out_directory);

        [DllImport("LEVCANlib", EntryPoint = "LCP_RequestDirectory", CharSet = CharSet.Ansi)]
        private static extern LC_Return lib_RequestDirectory(IntPtr mynode, byte from_node, ushort directory_index, ref lcpc_Directory_t out_directory);

        [DllImport("LEVCANlib", EntryPoint = "LCP_CleanDirectory", CharSet = CharSet.Ansi)]
        private static extern void lib_CleanDirectory(ref lcpc_Directory_t out_directory);

        [DllImport("LEVCANlib", EntryPoint = "LCP_SetValue", CharSet = CharSet.Ansi)]
        private static extern LC_Return lib_SetValue(IntPtr mynode, byte remote_node, ushort directory_index, ushort entry_index, IntPtr value, ushort valueSize);

        [DllImport("LEVCANlib", EntryPoint = "LCP_RequestValue", CharSet = CharSet.Ansi)]
        private static extern LC_Return lib_RequestValue(IntPtr mynode, byte from_node, ushort directory_index, ushort entry_index, IntPtr outVariable, ushort varSize);

        [DllImport("LEVCANlib", EntryPoint = "LCP_ParameterClientInit", CharSet = CharSet.Ansi)]
        private static extern LC_Return lib_ParameterClientInit(IntPtr node);

        LC_Node myNode;
        byte remoteNode;
        Encoding remoteEncoding;
        public List<LCPC_Directory> Directories { get; private set; }
        //can be requested only once, otherwise collision will happen
        static Mutex sync = new Mutex(false);
        bool toBeDisposed = false;
        public bool ToBeDisposed { get => toBeDisposed; }
        public Encoding Encoding { get => remoteEncoding; }

        public LC_ParamClient(LC_Node node, ushort fromID, Encoding enc = null)
        {
            myNode = node;
            remoteNode = (byte)fromID;
            Directories = new List<LCPC_Directory>();

            if (enc == null)
            {
                var sname = node.GetNodeShortName(fromID);
                if (sname.NodeID == (ushort)LC_Address.Broadcast)
                    remoteEncoding = node.ShortName.CodePage;//not found, use own codepage
                else
                    remoteEncoding = sname.CodePage; //use sender codepage to decode
            }
            else
                remoteEncoding = enc;

            lib_ParameterClientInit(node.DescriptorPtr);

            node.AddressChanges += node_AddressChanges;
        }

        private void node_AddressChanges(object? sender, AddressChangeArgs e)
        {
            if (e.ShortName.NodeID == remoteNode && (e.State == LC_AddressState.Changed || e.State == LC_AddressState.Deleted))
            {
                //this node lost it's ID
                toBeDisposed = true;
                //unsubscribe
                if (sender != null && typeof(LC_Node) == sender.GetType())
                    ((LC_Node)sender).AddressChanges -= node_AddressChanges;
            }
        }

        Mutex updateOne = new Mutex(false);
        public async Task UpdateDirectoriesAsync()
        {
            if (toBeDisposed)
                return;
            if (updateOne.WaitOne(1))
            {
                await Task.Factory.StartNew(() => { UpdateDirectories(); });
                await UpdateEntriesAsync(0);
                await UpdateEntriesAsync(); //update rest
                updateOne.ReleaseMutex();
            }
            else
            {
            }
        }

        public void UpdateDirectories()
        {
            if (toBeDisposed)
                return;

            List<LCPC_Directory> dirs = new List<LCPC_Directory>();
            LC_Return state = LC_Return.Ok;
            lcpc_Directory_t dir_struct = new lcpc_Directory_t(); //buffer
                                                                  //scan all dirs
            ushort index = 0;
            int outofrange = 0;
            while (state != LC_Return.Timeout)
            {
                sync.WaitOne();
                state = lib_RequestDirectory(myNode.DescriptorPtr, remoteNode, index, ref dir_struct);
                sync.ReleaseMutex();

                if (state == LC_Return.Ok)
                {
                    var dirNew = new LCPC_Directory(dir_struct, remoteEncoding);
                    lib_CleanDirectory(ref dir_struct); //cleanup buffer structure
                    dirs.Add(dirNew);
                }
                else if (state == LC_Return.AccessError)
                {
                    //create just empty one
                    dirs.Add(new LCPC_Directory(index));
                }
                else if (state == LC_Return.OutOfRange)
                {
                    outofrange++;
                    //levcan bug dirsize =0 => outofrange
                    if (outofrange > 1)
                        state = LC_Return.Timeout;
                    else
                    {
                        //create just empty one
                        dirs.Add(new LCPC_Directory(index));
                    }
                }

                index++;
            }
            Directories = dirs;
        }

        public async Task UpdateEntriesAsync(int dirIndex = -1)
        {
            if (toBeDisposed)
                return;

            LC_Return ret = LC_Return.Ok;
            int from = 0, to = Directories.Count;
            if (dirIndex != -1)
            {
                from = dirIndex;
                to = dirIndex + 1;
            }
            for (int i = from; i < to && i < Directories.Count; i++)
            {
                var dir = Directories[i];
                if (dir.Entries == null || dir.Entries.Count != 0)
                    continue;

                for (ushort entryIndex = 0; dir.Index >= 0 && entryIndex < dir.MaxEntries; entryIndex++)
                {
                    var val = await Task.Factory.StartNew<LCPC_Entry>(() =>
                    {
                        return GetEntry((ushort)dir.Index, entryIndex, out ret);
                    });

                    if (val != null)
                    {
                        dir.Entries.Add(val);
                        if (val.Index > entryIndex)
                            entryIndex = val.Index;
                    }
                    //unexpected stop
                    if (ret == LC_Return.OutOfRange)
                        break;
                }
            }
        }

        public LCPC_Entry GetEntry(ushort dirIndex, ushort entryIndex, out LC_Return result)
        {
            if (toBeDisposed)
            {
                result = LC_Return.NodeOffline;
                return null;
            }

            lcpc_Entry_t entry = new lcpc_Entry_t();

            sync.WaitOne();
            result = lib_requestEntry(myNode.DescriptorPtr, remoteNode, dirIndex, entryIndex, ref entry);
            sync.ReleaseMutex();

            if (result == LC_Return.Ok)
            {
                var entryNew = new LCPC_Entry(entry, remoteEncoding, dirIndex, this);
                lib_CleanEntry(ref entry); //cleanup buffer structure
                return entryNew;
            }
            return null;
        }

        public async Task<LC_Return> UpdateEntryValue(LCPC_Entry entry)
        {
            if (toBeDisposed)
                return LC_Return.NodeOffline;

            var val = await Task.Factory.StartNew<LC_Return>(() =>
            {
                sync.WaitOne();
                LC_Return result = lib_RequestValue(myNode.DescriptorPtr, remoteNode, entry.ParentDirectory, entry.Index, entry.VariablePtr, entry.VariableSize);
                sync.ReleaseMutex();
                return result;
            });

            return val;
        }

        public async Task<LC_Return> SendEntryValue(LCPC_Entry entry)
        {
            if (toBeDisposed)
                return LC_Return.NodeOffline;

            var val = await Task.Factory.StartNew<LC_Return>(() =>
            {
                sync.WaitOne();
                LC_Return result = lib_SetValue(myNode.DescriptorPtr, remoteNode, entry.ParentDirectory, entry.Index, entry.VariablePtr, entry.VariableSize);
                sync.ReleaseMutex();
                return result;
            });

            return val;
        }
    }

    public class LCPC_Entry : IDisposable
    {
        public string Name { get; }
        public string? TextData { get; }
        public object Descriptor { get; }
        public ushort Index { get; }
        public ushort VariableSize { get; }
        public ushort FolderDirIndex { get { return VariableSize; } }
        public ushort ParentDirectory { get; }

        public LCP_EntryType EType { get; }
        public LCP_Mode Mode { get; }

        public IntPtr VariablePtr { get; }
        LC_ParamClient client;
        string[] textSplitData;

        internal unsafe LCPC_Entry(lcpc_Entry_t entry, Encoding encoding, ushort parent, LC_ParamClient pclient)
        {
            client = pclient;
            ParentDirectory = parent;

            Name = Text8z.PtrToString(entry.Name, encoding, entry.TextSize);
            TextData = Text8z.PtrToString(entry.TextData, encoding, entry.TextSize);
            Index = entry.EntryIndex;
            EType = (LCP_EntryType)entry.EntryType;
            Mode = (LCP_Mode)entry.Mode;
            VariableSize = entry.VarSize;
            //decode descriptor
            Type t = null;
            switch (EType)
            {
                case LCP_EntryType.Folder:
                    break;
                case LCP_EntryType.Label:
                    break;
                case LCP_EntryType.Bool:
                    break;
                case LCP_EntryType.Enum:
                    t = typeof(LCP_Enum);
                    break;
                case LCP_EntryType.Bitfield32:
                    t = typeof(LCP_Bitfield32);
                    break;
                case LCP_EntryType.Int32:
                    t = typeof(LCP_Int32);
                    TextData = TextData?.Replace("%s", "%d");
                    break;
                case LCP_EntryType.Uint32:
                    t = typeof(LCP_Uint32);
                    TextData = TextData?.Replace("%s", "%u");
                    break;
                case LCP_EntryType.Int64:
                    t = typeof(LCP_Int64);
                    TextData = TextData?.Replace("%s", "%dl");
                    break;
                case LCP_EntryType.Uint64:
                    t = typeof(LCP_Uint64);
                    TextData = TextData?.Replace("%s", "%ul");
                    break;
                case LCP_EntryType.Float:
                    t = typeof(LCP_Float);
                    TextData = TextData?.Replace("%s", "%.3f");
                    break;
                case LCP_EntryType.Double:
                    TextData = TextData?.Replace("%s", "%.6f");
                    t = typeof(LCP_Double);
                    break;
                case LCP_EntryType.Decimal32:
                    t = typeof(LCP_Decimal32);
                    break;
                case LCP_EntryType.String:
                    t = typeof(LCP_String);
                    break;
                default:
                    break;
            }
            if (t != null && entry.Descriptor != null)
                Descriptor = Marshal.PtrToStructure(entry.Descriptor, t);
            if (VariableSize > 0 && EType != LCP_EntryType.Folder)
            {
                byte[] arr = new byte[VariableSize];
                VariablePtr = Marshal.AllocHGlobal(VariableSize);
                //copy to new location in a weird way
                Marshal.Copy(entry.Variable, arr, 0, VariableSize);
                Marshal.Copy(arr, 0, VariablePtr, VariableSize);
            }
        }
        public override string ToString()
        {
            return Name;
        }

        public async Task UpdateVariable()
        {
            await client.UpdateEntryValue(this);
        }

        public async Task SendVariable()
        {
            await client.SendEntryValue(this);
        }

        public string[] TextDataAsArray
        {
            get
            {
                if (textSplitData == null && TextData != null)
                {
                    textSplitData = TextData.Split('\n');
                }
                return textSplitData;
            }
        }

        public unsafe object Variable
        {
            get
            {
                object var = null;
                if (EType == LCP_EntryType.Int32 || EType == LCP_EntryType.Int64 || EType == LCP_EntryType.Decimal32)
                {
                    switch (VariableSize)
                    {
                        case 1:
                            var = (int)*(sbyte*)VariablePtr;
                            break;
                        case 2:
                            var = (int)*(short*)VariablePtr;
                            break;
                        case 4:
                            var = *(int*)VariablePtr;
                            break;
                        case 8:
                            var = *(long*)VariablePtr;
                            break;
                    }
                }
                else if (EType == LCP_EntryType.Bitfield32 || EType == LCP_EntryType.Enum || EType == LCP_EntryType.Uint32 || EType == LCP_EntryType.Uint64)
                {
                    switch (VariableSize)
                    {
                        case 1:
                            var = (uint)(*(byte*)VariablePtr);
                            break;
                        case 2:
                            var = (uint)(*(ushort*)VariablePtr);
                            break;
                        case 4:
                            var = *(uint*)VariablePtr;
                            break;
                        case 8:
                            var = *(ulong*)VariablePtr;
                            break;

                    }
                }
                else if (EType == LCP_EntryType.Float)
                {
                    if (VariableSize == 4)
                    {
                        var = *(float*)VariablePtr;
                    }
                }
                else if (EType == LCP_EntryType.Double)
                {
                    if (VariableSize == 8)
                    {
                        var = *(double*)VariablePtr;
                    }
                }
                else if (EType == LCP_EntryType.Bool)
                {
                    switch (VariableSize)
                    {
                        case 1:
                            var = (uint)(*(byte*)VariablePtr) > 0;
                            break;
                        case 2:
                            var = (uint)(*(ushort*)VariablePtr) > 0;
                            break;
                        case 4:
                            var = *(uint*)VariablePtr > 0;
                            break;
                        case 8:
                            var = *(ulong*)VariablePtr > 0;
                            break;
                    }
                }
                else if (EType == LCP_EntryType.String)
                {
                    LCP_String parm = (LCP_String)Descriptor;
                    if (parm.Flags.HasFlag(LCP_StringFlags.ASCII))
                    {
                        var = Encoding.ASCII.GetString((byte*)VariablePtr, VariableSize);
                    }
                    else if (parm.Flags.HasFlag(LCP_StringFlags.UTF8))
                    {
                        var = Encoding.UTF8.GetString((byte*)VariablePtr, VariableSize);
                    }
                    else if (parm.Flags.HasFlag(LCP_StringFlags.UTF16))
                    {
                        var = Encoding.Unicode.GetString((byte*)VariablePtr, VariableSize);
                    }
                    else
                    {
                        var = client.Encoding.GetString((byte*)VariablePtr, VariableSize);
                    }
                }

                return var;
            }

            set
            {
                if (EType == LCP_EntryType.Int32 || EType == LCP_EntryType.Int64 || EType == LCP_EntryType.Decimal32)
                {
                    long valueFixed = Convert.ToInt64(value);
                    switch (EType)
                    {
                        case LCP_EntryType.Decimal32:
                            if (valueFixed < ((LCP_Decimal32)Descriptor).Min)
                                valueFixed = ((LCP_Decimal32)Descriptor).Min;
                            if (valueFixed > ((LCP_Decimal32)Descriptor).Max)
                                valueFixed = ((LCP_Decimal32)Descriptor).Max;
                            break;
                        case LCP_EntryType.Int32:
                            if (valueFixed < ((LCP_Int32)Descriptor).Min)
                                valueFixed = ((LCP_Int32)Descriptor).Min;
                            if (valueFixed > ((LCP_Int32)Descriptor).Max)
                                valueFixed = ((LCP_Int32)Descriptor).Max;
                            break;
                        case LCP_EntryType.Int64:
                            if (valueFixed < ((LCP_Int64)Descriptor).Min)
                                valueFixed = ((LCP_Int64)Descriptor).Min;
                            if (valueFixed > ((LCP_Int64)Descriptor).Max)
                                valueFixed = ((LCP_Int64)Descriptor).Max;
                            break;
                    }
                    switch (VariableSize)
                    {
                        case 1:
                            *(sbyte*)VariablePtr = (sbyte)valueFixed;
                            break;
                        case 2:
                            *(short*)VariablePtr = (short)valueFixed;
                            break;
                        case 4:
                            *(int*)VariablePtr = (int)valueFixed;
                            break;
                        case 8:
                            *(long*)VariablePtr = (long)valueFixed;
                            break;
                    }
                }
                else if (EType == LCP_EntryType.Bitfield32 || EType == LCP_EntryType.Enum || EType == LCP_EntryType.Uint32 || EType == LCP_EntryType.Uint64)
                {
                    ulong valueFixed = Convert.ToUInt64(value);
                    switch (EType)
                    {
                        case LCP_EntryType.Bitfield32:
                            valueFixed = ((LCP_Bitfield32)Descriptor).Mask & valueFixed;
                            break;
                        case LCP_EntryType.Enum:
                            if (valueFixed < ((LCP_Enum)Descriptor).Min)
                                valueFixed = ((LCP_Enum)Descriptor).Min;
                            if (valueFixed > ((LCP_Enum)Descriptor).Min + ((LCP_Enum)Descriptor).Size)
                                valueFixed = ((LCP_Enum)Descriptor).Min + ((LCP_Enum)Descriptor).Size;
                            break;
                        case LCP_EntryType.Uint32:
                            if (valueFixed < ((LCP_Uint32)Descriptor).Min)
                                valueFixed = ((LCP_Uint32)Descriptor).Min;
                            if (valueFixed > ((LCP_Uint32)Descriptor).Max)
                                valueFixed = ((LCP_Uint32)Descriptor).Max;
                            break;
                        case LCP_EntryType.Uint64:
                            if (valueFixed < ((LCP_Uint64)Descriptor).Min)
                                valueFixed = ((LCP_Uint64)Descriptor).Min;
                            if (valueFixed > ((LCP_Uint64)Descriptor).Max)
                                valueFixed = ((LCP_Uint64)Descriptor).Max;
                            break;
                    }
                    switch (VariableSize)
                    {
                        case 1:
                            *(byte*)VariablePtr = Convert.ToByte(valueFixed);
                            break;
                        case 2:
                            *(ushort*)VariablePtr = Convert.ToUInt16(valueFixed);
                            break;
                        case 4:
                            *(uint*)VariablePtr = Convert.ToUInt32(valueFixed);
                            break;
                        case 8:
                            *(ulong*)VariablePtr = Convert.ToUInt64(valueFixed);
                            break;

                    }
                }
                else if (EType == LCP_EntryType.Float)
                {
                    float valueFixed = (float)value;
                    if (valueFixed < ((LCP_Float)Descriptor).Min)
                        valueFixed = ((LCP_Float)Descriptor).Min;
                    if (valueFixed > ((LCP_Float)Descriptor).Max)
                        valueFixed = ((LCP_Float)Descriptor).Max;

                    if (VariableSize == 4)
                    {
                        *(float*)VariablePtr = valueFixed;
                    }
                }
                else if (EType == LCP_EntryType.Double)
                {
                    double valueFixed = (double)value;
                    if (valueFixed < ((LCP_Double)Descriptor).Min)
                        valueFixed = ((LCP_Double)Descriptor).Min;
                    if (valueFixed > ((LCP_Double)Descriptor).Max)
                        valueFixed = ((LCP_Double)Descriptor).Max;
                    if (VariableSize == 8)
                    {
                        *(double*)VariablePtr = valueFixed;
                    }
                }
                else if (EType == LCP_EntryType.Bool)
                {
                    switch (VariableSize)
                    {
                        case 1:
                            *(byte*)VariablePtr = (byte)((bool)value ? 1u : 0);
                            break;
                        case 2:
                            *(ushort*)VariablePtr = (ushort)((bool)value ? 1u : 0);
                            break;
                        case 4:
                            *(uint*)VariablePtr = ((bool)value ? 1u : 0);
                            break;
                        case 8:
                            *(ulong*)VariablePtr = ((bool)value ? 1u : 0);
                            break;

                    }
                }
                else if (EType == LCP_EntryType.String)
                {
                }
            }
        }

        public void Dispose()
        {
            if (VariablePtr != null)
            {
                Marshal.FreeHGlobal(VariablePtr);
            }
        }
    }

    public class LCPC_Directory
    {
        public List<LCPC_Entry> Entries { get; internal set; }
        public string Name { get; }
        public int Index { get; }
        public ushort MaxEntries { get; }

        internal LCPC_Directory(lcpc_Directory_t dir, Encoding encoding)
        {
            Name = Text8z.PtrToString(dir.Name, encoding);
            Index = dir.DirectoryIndex;
            MaxEntries = dir.Size;
            Entries = new List<LCPC_Entry>(MaxEntries);
        }

        public LCPC_Directory(ushort index)
        {
            Name = "";
            Index = index;
            MaxEntries = 0;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
