using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using LEVCAN;

namespace LEVCAN.Tests
{
    /// <summary>
    /// A simulated uLight device that runs as a proper LEVCAN node on a
    /// <see cref="VirtualCanBus"/>.  It exposes a native parameter server with
    /// four directories (Root, Menu, Inputs, About) modelled after the real
    /// firmware layout in <c>directories.h</c>.
    ///
    /// The native <c>LCP_ParameterServerInit</c> function reads
    /// <c>node->Directories</c> and <c>node->DirectoriesSize</c> from the
    /// node descriptor. Both fields are written via the clean native
    /// <see cref="LC_Node.LC_Node_SetDirectories"/> API call.
    /// </summary>
    public sealed class FakeULightDevice : IDisposable
    {
        // ── P/Invoke ──────────────────────────────────────────────────────────

        [DllImport("LEVCANlib", EntryPoint = "LCP_ParameterServerInit",
            CallingConvention = CallingConvention.StdCall)]
        private static extern int LCP_ParameterServerInit(IntPtr node, IntPtr callback);

        // ── Native struct mirrors ─────────────────────────────────────────────
        //  LCPS_Entry_t  (levcan_paramserver.h)
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeEntry
        {
            public IntPtr Variable;   // const void*
            public IntPtr Descriptor; // const void* (depends on LCP_Type_t)
            public IntPtr Name;       // const char*
            public IntPtr TextData;   // const char*
            public ushort VarSize;    // size in bytes, OR dir-index for Folder
            public ushort DescSize;   // size of descriptor struct
            public byte   EntryType;  // LCP_Type_t
            public byte   AccessLvl;  // LCP_AccessLvl_t
            public byte   Mode;       // LCP_Mode_t
            public byte   Reserved;
        }

        //  LCPS_Directory_t  (levcan_paramserver.h)
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeDirectory
        {
            public IntPtr Entries;    // const LCPS_Entry_t*
            public IntPtr Name;       // const char*
            public ushort Size;       // entry count
            public byte   ArrayIndex;
            public byte   AccessLvl;
        }

        // LCP_Type_t values
        private const byte LCP_Folder  = 0;
        private const byte LCP_Label   = 1;
        private const byte LCP_Bool    = 2;
        private const byte LCP_Float   = 9;
        // LCP_Mode_t flags
        private const byte LCP_Normal     = 0;
        private const byte LCP_ReadOnly   = 1;
        private const byte LCP_LiveUpdate = 1 << 2;
        private const byte LCP_ROLiveUpd  = LCP_ReadOnly | LCP_LiveUpdate;
        private const byte LCP_NLiveUpd   = LCP_Normal | LCP_LiveUpdate;
        // LCP_AccessLvl_t
        private const byte LCP_AccessLvl_Any = 0;

        // Directory indices (order must match the NativeDirectory[] array built below)
        private const int DIR_ROOT   = 0;
        private const int DIR_MENU   = 1;
        private const int DIR_INPUTS = 2;
        private const int DIR_ABOUT  = 3;

        // ── LCP_Float_t descriptor ────────────────────────────────────────────
        [StructLayout(LayoutKind.Sequential)]
        private struct FloatDesc { public float Min, Max, Step; }

        // ── Live parameter values (pinned so native code can safely read) ─────

        // Inputs
        private readonly float[] _voltage    = { 12.5f };
        private readonly float[] _current    = { 3.14f };
        private readonly float[] _tempInt    = { 36.0f };
        private readonly byte[]  _btn1       = { 0 };
        private readonly byte[]  _btn2       = { 0 };
        // Menu
        private readonly byte[]  _menuSave      = { 0 };
        private readonly byte[]  _menuReboot    = { 0 };
        private readonly byte[]  _menuLoadDef   = { 0 };
        // Float descriptors
        private FloatDesc _fdVoltage = new FloatDesc { Min =  0,   Max = 100, Step = 0.001f };
        private FloatDesc _fdCurrent = new FloatDesc { Min = -100, Max = 100, Step = 0.001f };
        private FloatDesc _fdTemp    = new FloatDesc { Min = -40,  Max = 150, Step = 0.1f };

        private readonly GCHandle _hVoltage, _hCurrent, _hTempInt;
        private readonly GCHandle _hBtn1, _hBtn2;
        private readonly GCHandle _hMenuSave, _hMenuReboot, _hMenuLoadDef;
        private readonly GCHandle _hFdVoltage, _hFdCurrent, _hFdTemp;

        // All unmanaged allocs, freed on Dispose
        private readonly List<IntPtr> _nativeAllocs = new List<IntPtr>();

        // ── Public surface ────────────────────────────────────────────────────

        public LC_Node Node { get; private set; }

        private readonly VirtualCanBus _bus;
        private bool _disposed;

        // ── Constructor ───────────────────────────────────────────────────────

        public FakeULightDevice(VirtualCanBus bus, byte nodeId = 5)
        {
            _bus = bus;

            // Pin managed arrays before handing addresses to native code.
            _hVoltage  = GCHandle.Alloc(_voltage,   GCHandleType.Pinned);
            _hCurrent  = GCHandle.Alloc(_current,   GCHandleType.Pinned);
            _hTempInt  = GCHandle.Alloc(_tempInt,   GCHandleType.Pinned);
            _hBtn1     = GCHandle.Alloc(_btn1,      GCHandleType.Pinned);
            _hBtn2     = GCHandle.Alloc(_btn2,      GCHandleType.Pinned);
            _hMenuSave    = GCHandle.Alloc(_menuSave,    GCHandleType.Pinned);
            _hMenuReboot  = GCHandle.Alloc(_menuReboot,  GCHandleType.Pinned);
            _hMenuLoadDef = GCHandle.Alloc(_menuLoadDef, GCHandleType.Pinned);
            _hFdVoltage = GCHandle.Alloc(_fdVoltage, GCHandleType.Pinned);
            _hFdCurrent = GCHandle.Alloc(_fdCurrent, GCHandleType.Pinned);
            _hFdTemp    = GCHandle.Alloc(_fdTemp,    GCHandleType.Pinned);

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Create node; its identity is set via the clean native API.
            Node = new LC_Node(nodeId);
            SetNodeStrings();

            // Expose node-name object so address discovery replies work, and Speed object for data request test.
            var nameObj = new LC_ObjectString(
                (ushort)LC_SystemMessage.NodeName, "FakeULight", 128,
                LC_ObjectAttributes.Readable | LC_ObjectAttributes.Writable);
            var speedObj = new LC_Object(
                (ushort)LC_Objects_Std.LC_Obj_Speed, new LC_Obj_Speed_t { Speed = 42 },
                LC_ObjectAttributes.Readable);
            Node.Objects = new LC_IObject[] { nameObj, speedObj };

            // Register with the bus: all inbound frames go to this node's receiver.
            _bus.Connect(Node,
                (hdr, data, len) => LC_Interface.lib_ReceiveHandler(Node.DescriptorPtr, hdr, data, len));

            // StartNode first (calls lib_createNode which sets up address discovery
            // and System objects), then install the parameter server on top.
            Node.StartNode();

            // Install the native parameter server AFTER the node is created so
            // lc_registerSystemObjects appends after the standard system objects.
            InstallParameterServer();
        }

        // ── Name setup via native API ─────────────────────────────────────────

        private void SetNodeStrings()
        {
            uint[] serial = new uint[] { 1, 2, 3, 4 };
            LC_Node.LC_Node_SetIdentity(Node.DescriptorPtr, "FakeULight", "FakeULight", "Nucular.tech", 1251, serial);
            var sn = Node.ShortName;
            sn.Configurable = true;
            sn.Variables    = true;
            Node.ShortName = sn;
        }

        // ── Parameter server installation ─────────────────────────────────────

        private void InstallParameterServer()
        {
            // Build entry arrays for each directory.
            IntPtr pRoot   = BuildEntryBlock(BuildRootEntries());
            IntPtr pMenu   = BuildEntryBlock(BuildMenuEntries());
            IntPtr pInputs = BuildEntryBlock(BuildInputsEntries());
            IntPtr pAbout  = BuildEntryBlock(BuildAboutEntries());

            // Build the LCPS_Directory_t array.
            var dirs = new NativeDirectory[4];
            dirs[DIR_ROOT]   = MakeDir(pRoot,   3, "uLight");
            dirs[DIR_MENU]   = MakeDir(pMenu,   3, "Menu");
            dirs[DIR_INPUTS] = MakeDir(pInputs, 5, "Inputs");
            dirs[DIR_ABOUT]  = MakeDir(pAbout,  2, "About");

            IntPtr pDirs = BuildDirBlock(dirs);

            // Set directories directly via the clean native API.
            LC_Node.LC_Node_SetDirectories(Node.DescriptorPtr, pDirs, (ushort)dirs.Length);

            // Register the parameter request handler in the native library.
            int rc = LCP_ParameterServerInit(Node.DescriptorPtr, IntPtr.Zero);
            if (rc != 0)
                throw new InvalidOperationException(
                    $"LCP_ParameterServerInit returned error code {rc}");
        }

        // ── Entry builders ────────────────────────────────────────────────────

        private NativeEntry[] BuildRootEntries() => new NativeEntry[]
        {
            FolderEntry((ushort)DIR_MENU,   "Menu"),
            FolderEntry((ushort)DIR_INPUTS, "Inputs"),
            FolderEntry((ushort)DIR_ABOUT,  "About"),
        };

        private NativeEntry[] BuildMenuEntries() => new NativeEntry[]
        {
            BoolEntry(_hMenuSave.AddrOfPinnedObject(),    "Save"),
            BoolEntry(_hMenuReboot.AddrOfPinnedObject(),  "Reboot"),
            BoolEntry(_hMenuLoadDef.AddrOfPinnedObject(), "Load defaults"),
        };

        private NativeEntry[] BuildInputsEntries() => new NativeEntry[]
        {
            FloatEntry(_hVoltage.AddrOfPinnedObject(), _hFdVoltage.AddrOfPinnedObject(),
                       "Voltage",       "%s V",   LCP_ROLiveUpd),
            FloatEntry(_hCurrent.AddrOfPinnedObject(), _hFdCurrent.AddrOfPinnedObject(),
                       "Current",       "%s A",   LCP_ROLiveUpd),
            FloatEntry(_hTempInt.AddrOfPinnedObject(), _hFdTemp.AddrOfPinnedObject(),
                       "Temp internal", "%s°C",  LCP_ROLiveUpd),
            BoolEntry(_hBtn1.AddrOfPinnedObject(), "Button 1"),
            BoolEntry(_hBtn2.AddrOfPinnedObject(), "Button 2"),
        };

        private NativeEntry[] BuildAboutEntries() => new NativeEntry[]
        {
            LabelEntry("Firmware ver."),
            LabelEntry("1.0.0"),
        };

        // ── Factory helpers ───────────────────────────────────────────────────

        private NativeEntry FolderEntry(ushort dirIndex, string name) =>
            new NativeEntry
            {
                Variable   = IntPtr.Zero,
                Descriptor = IntPtr.Zero,
                Name       = AllocString(name),
                TextData   = IntPtr.Zero,
                VarSize    = dirIndex,   // for Folder: VarSize = target directory index
                DescSize   = 0,
                EntryType  = LCP_Folder,
                AccessLvl  = LCP_AccessLvl_Any,
                Mode       = 0,
            };

        private NativeEntry LabelEntry(string name) =>
            new NativeEntry
            {
                Variable   = IntPtr.Zero,
                Descriptor = IntPtr.Zero,
                Name       = AllocString(name),
                TextData   = IntPtr.Zero,
                VarSize    = 0,
                DescSize   = 0,
                EntryType  = LCP_Label,
                AccessLvl  = LCP_AccessLvl_Any,
                Mode       = LCP_ReadOnly,
            };

        private NativeEntry BoolEntry(IntPtr variable, string name) =>
            new NativeEntry
            {
                Variable   = variable,
                Descriptor = IntPtr.Zero,
                Name       = AllocString(name),
                TextData   = IntPtr.Zero,
                VarSize    = 1,          // sizeof(bool) = 1 byte
                DescSize   = 0,
                EntryType  = LCP_Bool,
                AccessLvl  = LCP_AccessLvl_Any,
                Mode       = LCP_NLiveUpd,
            };

        private NativeEntry FloatEntry(IntPtr variable, IntPtr descriptor,
            string name, string textData, byte mode) =>
            new NativeEntry
            {
                Variable   = variable,
                Descriptor = descriptor,
                Name       = AllocString(name),
                TextData   = AllocString(textData),
                VarSize    = 4,                           // sizeof(float)
                DescSize   = (ushort)Marshal.SizeOf<FloatDesc>(),
                EntryType  = LCP_Float,
                AccessLvl  = LCP_AccessLvl_Any,
                Mode       = mode,
            };

        private NativeDirectory MakeDir(IntPtr entries, int size, string name) =>
            new NativeDirectory
            {
                Entries    = entries,
                Name       = AllocString(name),
                Size       = (ushort)size,
                ArrayIndex = 0,
                AccessLvl  = LCP_AccessLvl_Any,
            };

        // ── Unmanaged allocation helpers ──────────────────────────────────────

        private IntPtr AllocString(string s)
        {
            IntPtr p = Text8z.StringToPtr(s, Encoding.ASCII);
            _nativeAllocs.Add(p);
            return p;
        }

        private IntPtr BuildEntryBlock(NativeEntry[] entries)
        {
            int sz = Marshal.SizeOf<NativeEntry>();
            IntPtr p = Marshal.AllocHGlobal(sz * entries.Length);
            _nativeAllocs.Add(p);
            for (int i = 0; i < entries.Length; i++)
                Marshal.StructureToPtr(entries[i], p + i * sz, false);
            return p;
        }

        private IntPtr BuildDirBlock(NativeDirectory[] dirs)
        {
            int sz = Marshal.SizeOf<NativeDirectory>();
            IntPtr p = Marshal.AllocHGlobal(sz * dirs.Length);
            _nativeAllocs.Add(p);
            for (int i = 0; i < dirs.Length; i++)
                Marshal.StructureToPtr(dirs[i], p + i * sz, false);
            return p;
        }

        // ── IDisposable ───────────────────────────────────────────────────────

        public void SetVoltage(float v) => _voltage[0] = v;
        public byte Button1 => _btn1[0];

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _bus.Disconnect(Node);

            foreach (var h in new[]
            {
                _hVoltage, _hCurrent, _hTempInt, _hBtn1, _hBtn2,
                _hMenuSave, _hMenuReboot, _hMenuLoadDef,
                _hFdVoltage, _hFdCurrent, _hFdTemp,
            })
            {
                if (h.IsAllocated) h.Free();
            }

            foreach (var p in _nativeAllocs)
                if (p != IntPtr.Zero)
                    Marshal.FreeHGlobal(p);

            _nativeAllocs.Clear();
        }
    }
}
