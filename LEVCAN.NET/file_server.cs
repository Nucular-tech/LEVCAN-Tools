using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace LEVCAN
{
    public enum LC_FileAccess
    {
        Read = 0x01,          // Specifies read access to the object. Data can be read from the file.
        Write = 0x02,         // Specifies write access to the object. Data can be written to the file. Combine with Read for read-write access.
        OpenExisting = 0x00,  // Opens the file. The function fails if the file is not existing. (Default)
        CreateNew = 0x04,     // Creates a new file. The function fails with FR_EXIST if the file is existing.
        CreateAlways = 0x08,  // Creates a new file. If the file is existing, it will be truncated and overwritten.
        OpenAlways = 0x10,    // Opens the file if it is existing. If not, a new file will be created.
        OpenAppend = 0x30,    // Same as OpenAlways except the read/write pointer is set end of the file.
    };

    public enum LC_FileResult
    {
        Ok = 0,               /* (0) Succeeded */
        DiskErr,              /* (1) A hard error occurred in the low level disk I/O layer */
        IntErr,               /* (2) Assertion failed */
        NotReady,             /* (3) The physical drive cannot work */
        NoFile,               /* (4) Could not find the file */
        NoPath,               /* (5) Could not find the path */
        InvalidName,          /* (6) The path name format is invalid */
        Denied,               /* (7) Access denied due to prohibited access or directory full */
        Exist,                /* (8) Access denied due to prohibited access */
        InvalidObject,        /* (9) The file/directory object is invalid */
        WriteProtected,       /* (10) The physical drive is write protected */
        Reserved1,            /* (11 NOT USED) The logical drive number is invalid */
        Reserved2,            /* (12 NOT USED) The volume has no work area */
        Reserved3,            /* (13 NOT USED) There is no valid FAT volume */
        Reserved4,            /* (14 NOT USED) The f_mkfs() aborted due to any problem */
        Timeout,              /* (15) Could not get a grant to access the volume within defined period */
        Locked,               /* (16) The operation is rejected according to the file sharing policy */
        Reserved5,            /* (17 NOT USED) LFN working buffer could not be allocated */
        TooManyOpenFiles,     /* (18) Number of open files > FF_FS_LOCK */
        InvalidParameter,     /* (19) Given parameter is invalid */
        NetworkTimeout,       /* (20) Could not get access the node */
        NetworkError,         /* (21) Data corrupted during transmission */
        NetworkBusy,          /* (22) Buffer full */
        MemoryFull,           /* (23) Could not allocate data */
        NodeOffline,          /* (24) Node disabled */
        FileNotOpened,        /* (25) File was closed by timeout or it wasn't opened at all  */
    };

    public unsafe class LC_FileServer : IDisposable
    {
        private static FileMode ToFileMode(LC_FileAccess access)
        {
            if (access.HasFlag(LC_FileAccess.CreateNew))
                return FileMode.CreateNew;
            if (access.HasFlag(LC_FileAccess.CreateAlways))
                return FileMode.Create;
            if (access.HasFlag(LC_FileAccess.OpenAlways))
                return FileMode.OpenOrCreate;
            if (access.HasFlag(LC_FileAccess.OpenAppend))
                return FileMode.Append;
            return FileMode.Open;
        }

        private static FileAccess ToFileAccess(LC_FileAccess access)
        {
            FileAccess fa = 0;
            if (access.HasFlag(LC_FileAccess.Read))
                fa |= FileAccess.Read;
            if (access.HasFlag(LC_FileAccess.Write))
                fa |= FileAccess.Write;
            return fa == 0 ? FileAccess.Read : fa;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate LC_FileResult fOpen_d(IntPtr* fileObject, IntPtr name, LC_FileAccess mode);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint fTell_d(IntPtr fileObject);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate LC_FileResult fSeek_d(IntPtr fileObject, uint pointer);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate LC_FileResult fRead_d(IntPtr fileObject, byte* buffer, uint bytesToRead, uint* bytesReaded);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate LC_FileResult fWrite_d(IntPtr fileObject, byte* buffer, uint bytesToWrite, uint* bytesWritten);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate LC_FileResult fClose_d(IntPtr fileObject);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint fSize_d(IntPtr fileObject);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate LC_FileResult fTruncate_d(IntPtr fileObject);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void fOnReceive_d();

        // Pinned static delegate references to ensure the garbage collector NEVER collects them
        private static readonly fOpen_d s_fOpen = StaticFileOpen;
        private static readonly fTell_d s_fTell = StaticFileTell;
        private static readonly fSeek_d s_fSeek = StaticFileSeek;
        private static readonly fRead_d s_fRead = StaticFileRead;
        private static readonly fWrite_d s_fWrite = StaticFileWrite;
        private static readonly fClose_d s_fClose = StaticFileClose;
        private static readonly fSize_d s_fSize = StaticFileSize;
        private static readonly fTruncate_d s_fTruncate = StaticFileTruncate;
        private static readonly fOnReceive_d s_fOnReceive = StaticFileOnReceive;

        private static bool s_callbacksRegistered = false;
        private static readonly object s_callbacksLock = new();
        private static LC_FileServer? s_activeServer;

        [DllImport("LEVCANlib", EntryPoint = "LC_Set_FileCallbacks", CallingConvention = CallingConvention.StdCall)]
        private static extern void lib_setFileCallbacks(fOpen_d fopen, fTell_d ftell, fSeek_d flseek, fRead_d fread, fWrite_d fwrite, fClose_d fclose, fTruncate_d ftruncate, fSize_d fsize, fOnReceive_d onrec);

        [DllImport("LEVCANlib", EntryPoint = "LC_FileServerInit", CallingConvention = CallingConvention.StdCall)]
        private static extern LC_Return lib_FileServerInit(IntPtr node);

        [DllImport("LEVCANlib", EntryPoint = "LC_FileServer", CallingConvention = CallingConvention.StdCall)]
        private static extern void lib_FileServer(IntPtr node, uint tick);

        private readonly LC_Node _node;
        private readonly SemaphoreSlim _mutex = new(0, 100);
        private readonly Dictionary<int, FileStream> _files = new();
        private readonly object _filesLock = new();
        private int _fileIndex = 1;
        private string _savePath = "";
        private volatile bool _running = true;

        public string SavePath
        {
            get => _savePath;
            set
            {
                try
                {
                    if (!Directory.Exists(value))
                    {
                        Directory.CreateDirectory(value);
                    }
                    _savePath = Path.GetFullPath(value);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"LC_FileServer: failed to set SavePath '{value}': {ex.Message}");
                    _savePath = AppDomain.CurrentDomain.BaseDirectory;
                }
            }
        }

        public LC_FileServer(LC_Node node, string path)
        {
            _node = node ?? throw new ArgumentNullException(nameof(node));
            SavePath = path;

            InitCallbacks();

            if (_node.DescriptorPtr != IntPtr.Zero)
            {
                lib_FileServerInit(_node.DescriptorPtr);
            }

            var updates = new Thread(FileServerThread)
            {
                IsBackground = true,
                Name = "LEVCAN_FileServerThread"
            };
            updates.Start();
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private static void Log(string msg)
        {
            try
            {
                string logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "file_server_trace.log");
                File.AppendAllText(logFile, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}{Environment.NewLine}");
            }
            catch { }
        }

        private void InitCallbacks()
        {
            lock (s_callbacksLock)
            {
                s_activeServer = this;
                Log($"InitCallbacks: s_activeServer set. Callbacks registered = {s_callbacksRegistered}");
                if (!s_callbacksRegistered)
                {
                    lib_setFileCallbacks(s_fOpen, s_fTell, s_fSeek, s_fRead, s_fWrite, s_fClose, s_fTruncate, s_fSize, s_fOnReceive);
                    s_callbacksRegistered = true;
                    Log("lib_setFileCallbacks successfully invoked.");
                }
            }
        }

        #region Static Dispatchers (reverse P/Invoke entry points)

        private static LC_FileResult StaticFileOpen(IntPtr* fileObject, IntPtr name, LC_FileAccess mode)
        {
            Log($"StaticFileOpen called: fileObject=0x{(long)fileObject:X}, name=0x{name.ToInt64():X}, mode={mode}");
            if (fileObject != null) *fileObject = IntPtr.Zero;
            var srv = s_activeServer;
            if (srv == null)
            {
                Log("StaticFileOpen: s_activeServer is null!");
                return LC_FileResult.NotReady;
            }
            var res = srv.FileOpen(fileObject, name, mode);
            Log($"StaticFileOpen result: {res}");
            return res;
        }

        private static uint StaticFileTell(IntPtr fileObject)
        {
            Log($"StaticFileTell: fileObject=0x{fileObject.ToInt64():X}");
            var srv = s_activeServer;
            return srv != null ? srv.FileTell(fileObject) : 0;
        }

        private static LC_FileResult StaticFileSeek(IntPtr fileObject, uint pointer)
        {
            Log($"StaticFileSeek: fileObject=0x{fileObject.ToInt64():X}, pointer={pointer}");
            var srv = s_activeServer;
            return srv != null ? srv.FileSeek(fileObject, pointer) : LC_FileResult.NotReady;
        }

        private static LC_FileResult StaticFileRead(IntPtr fileObject, byte* buffer, uint bytesToRead, uint* bytesReaded)
        {
            Log($"StaticFileRead: fileObject=0x{fileObject.ToInt64():X}, bytesToRead={bytesToRead}");
            if (bytesReaded != null) *bytesReaded = 0;
            var srv = s_activeServer;
            return srv != null ? srv.FileRead(fileObject, buffer, bytesToRead, bytesReaded) : LC_FileResult.NotReady;
        }

        private static LC_FileResult StaticFileWrite(IntPtr fileObject, byte* buffer, uint bytesToWrite, uint* bytesWritten)
        {
            Log($"StaticFileWrite: fileObject=0x{fileObject.ToInt64():X}, bytesToWrite={bytesToWrite}");
            if (bytesWritten != null) *bytesWritten = 0;
            var srv = s_activeServer;
            return srv != null ? srv.FileWrite(fileObject, buffer, bytesToWrite, bytesWritten) : LC_FileResult.NotReady;
        }

        private static LC_FileResult StaticFileClose(IntPtr fileObject)
        {
            Log($"StaticFileClose: fileObject=0x{fileObject.ToInt64():X}");
            var srv = s_activeServer;
            return srv != null ? srv.FileClose(fileObject) : LC_FileResult.NotReady;
        }

        private static uint StaticFileSize(IntPtr fileObject)
        {
            Log($"StaticFileSize: fileObject=0x{fileObject.ToInt64():X}");
            var srv = s_activeServer;
            return srv != null ? srv.FileSize(fileObject) : 0;
        }

        private static LC_FileResult StaticFileTruncate(IntPtr fileObject)
        {
            Log($"StaticFileTruncate: fileObject=0x{fileObject.ToInt64():X}");
            var srv = s_activeServer;
            return srv != null ? srv.FileTruncate(fileObject) : LC_FileResult.NotReady;
        }

        private static void StaticFileOnReceive()
        {
            Log("StaticFileOnReceive invoked from native code");
            var srv = s_activeServer;
            srv?.FileOnReceive();
        }

        #endregion

        #region Instance Handlers (Safe & Exception-Contained)

        private LC_FileResult FileOpen(IntPtr* fileObject, IntPtr name, LC_FileAccess mode)
        {
            if (fileObject == null)
                return LC_FileResult.InvalidParameter;

            *fileObject = IntPtr.Zero;

            if (name == IntPtr.Zero)
                return LC_FileResult.InvalidName;

            try
            {
                Encoding enc = Encoding.UTF8;
                try
                {
                    if (_node != null)
                        enc = _node.ShortName.CodePage ?? Encoding.UTF8;
                }
                catch
                {
                    enc = Encoding.UTF8;
                }

                string? file = Text8z.PtrToString(name, enc, 512);
                if (string.IsNullOrWhiteSpace(file))
                    return LC_FileResult.InvalidName;

                // Normalize path separators and prevent directory traversal
                file = file.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
                while (file.Contains(".."))
                {
                    file = file.Replace("..", "");
                }

                file = file.TrimStart(Path.DirectorySeparatorChar);
                if (string.IsNullOrWhiteSpace(file))
                    return LC_FileResult.InvalidName;

                string fullPath = Path.GetFullPath(Path.Combine(_savePath, file));
                Log($"FileOpen: parsed filename='{file}', fullPath='{fullPath}', mode={mode}");
                if (!fullPath.StartsWith(_savePath, StringComparison.OrdinalIgnoreCase))
                {
                    Log($"FileOpen: path denied (not under savePath '{_savePath}')");
                    return LC_FileResult.Denied;
                }

                // Ensure directory exists if filename has subdirectories
                string? dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                FileStream fs = new FileStream(fullPath, ToFileMode(mode), ToFileAccess(mode), FileShare.ReadWrite);

                lock (_filesLock)
                {
                    int handle = _fileIndex++;
                    *fileObject = new IntPtr(handle);
                    _files[handle] = fs;
                    Log($"FileOpen: successfully opened handle {handle}");
                }

                return LC_FileResult.Ok;
            }
            catch (FileNotFoundException)
            {
                return LC_FileResult.NoFile;
            }
            catch (DirectoryNotFoundException)
            {
                return LC_FileResult.NoPath;
            }
            catch (UnauthorizedAccessException)
            {
                return LC_FileResult.Denied;
            }
            catch (PathTooLongException)
            {
                return LC_FileResult.InvalidName;
            }
            catch (IOException)
            {
                return ToFileMode(mode) == FileMode.CreateNew ? LC_FileResult.Exist : LC_FileResult.IntErr;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LC_FileServer: FileOpen exception: {ex.Message}");
                return LC_FileResult.IntErr;
            }
        }

        private uint FileTell(IntPtr fileObject)
        {
            try
            {
                int key = fileObject.ToInt32();
                lock (_filesLock)
                {
                    if (_files.TryGetValue(key, out var fs))
                        return (uint)fs.Position;
                }
            }
            catch { }
            return 0;
        }

        private LC_FileResult FileSeek(IntPtr fileObject, uint pointer)
        {
            try
            {
                int key = fileObject.ToInt32();
                FileStream? fs;
                lock (_filesLock)
                {
                    _files.TryGetValue(key, out fs);
                }

                if (fs == null)
                    return LC_FileResult.FileNotOpened;

                fs.Seek(pointer, SeekOrigin.Begin);
                return LC_FileResult.Ok;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LC_FileServer: FileSeek exception: {ex.Message}");
                return LC_FileResult.IntErr;
            }
        }

        private LC_FileResult FileRead(IntPtr fileObject, byte* buffer, uint bytesToRead, uint* bytesReaded)
        {
            if (bytesReaded != null)
                *bytesReaded = 0;

            if (buffer == null || bytesReaded == null)
                return LC_FileResult.InvalidParameter;

            if (bytesToRead == 0)
                return LC_FileResult.Ok;

            try
            {
                int key = fileObject.ToInt32();
                FileStream? fs;
                lock (_filesLock)
                {
                    _files.TryGetValue(key, out fs);
                }

                if (fs == null)
                    return LC_FileResult.FileNotOpened;

                byte[] buff = new byte[bytesToRead];
                int read = fs.Read(buff, 0, (int)bytesToRead);
                if (read > 0)
                {
                    Marshal.Copy(buff, 0, (IntPtr)buffer, read);
                }
                *bytesReaded = (uint)read;
                return LC_FileResult.Ok;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LC_FileServer: FileRead exception: {ex.Message}");
                return LC_FileResult.IntErr;
            }
        }

        private LC_FileResult FileWrite(IntPtr fileObject, byte* buffer, uint bytesToWrite, uint* bytesWritten)
        {
            if (bytesWritten != null)
                *bytesWritten = 0;

            if (buffer == null || bytesWritten == null)
                return LC_FileResult.InvalidParameter;

            if (bytesToWrite == 0)
                return LC_FileResult.Ok;

            try
            {
                int key = fileObject.ToInt32();
                FileStream? fs;
                lock (_filesLock)
                {
                    _files.TryGetValue(key, out fs);
                }

                if (fs == null)
                    return LC_FileResult.FileNotOpened;

                byte[] buff = new byte[bytesToWrite];
                Marshal.Copy((IntPtr)buffer, buff, 0, (int)bytesToWrite);
                fs.Write(buff, 0, (int)bytesToWrite);
                fs.Flush();
                *bytesWritten = bytesToWrite;
                return LC_FileResult.Ok;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LC_FileServer: FileWrite exception: {ex.Message}");
                return LC_FileResult.IntErr;
            }
        }

        private LC_FileResult FileClose(IntPtr fileObject)
        {
            try
            {
                int key = fileObject.ToInt32();
                FileStream? fs;
                lock (_filesLock)
                {
                    if (_files.TryGetValue(key, out fs))
                    {
                        _files.Remove(key);
                    }
                }

                if (fs == null)
                    return LC_FileResult.FileNotOpened;

                fs.Flush();
                fs.Dispose();
                return LC_FileResult.Ok;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LC_FileServer: FileClose exception: {ex.Message}");
                return LC_FileResult.IntErr;
            }
        }

        private uint FileSize(IntPtr fileObject)
        {
            try
            {
                int key = fileObject.ToInt32();
                lock (_filesLock)
                {
                    if (_files.TryGetValue(key, out var fs))
                        return (uint)fs.Length;
                }
            }
            catch { }
            return 0;
        }

        private LC_FileResult FileTruncate(IntPtr fileObject)
        {
            try
            {
                int key = fileObject.ToInt32();
                FileStream? fs;
                lock (_filesLock)
                {
                    _files.TryGetValue(key, out fs);
                }

                if (fs == null)
                    return LC_FileResult.FileNotOpened;

                fs.SetLength(fs.Position);
                return LC_FileResult.Ok;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LC_FileServer: FileTruncate exception: {ex.Message}");
                return LC_FileResult.IntErr;
            }
        }

        private void FileOnReceive()
        {
            try
            {
                if (_mutex.CurrentCount == 0)
                    _mutex.Release();
            }
            catch { }
        }

        private void FileServerThread()
        {
            var watch = new Stopwatch();
            watch.Start();

            while (_running)
            {
                try
                {
                    _mutex.Wait(100);
                    watch.Stop();
                    uint elaps = (uint)watch.ElapsedMilliseconds;
                    watch.Restart();

                    if (_node != null && _node.DescriptorPtr != IntPtr.Zero)
                    {
                        lib_FileServer(_node.DescriptorPtr, elaps);
                    }
                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"LC_FileServer: FileServerThread error: {ex.Message}");
                }
            }
        }

        #endregion

        public void Dispose()
        {
            _running = false;
            try
            {
                _mutex.Release();
            }
            catch { }

            lock (_filesLock)
            {
                foreach (var fs in _files.Values)
                {
                    try
                    {
                        fs.Flush();
                        fs.Dispose();
                    }
                    catch { }
                }
                _files.Clear();
            }

            lock (s_callbacksLock)
            {
                if (s_activeServer == this)
                {
                    s_activeServer = null;
                }
            }
        }
    }
}
