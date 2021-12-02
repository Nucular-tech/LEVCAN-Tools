using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

    public unsafe class LC_FileServer
    {
        private static FileMode ToFileMode(LC_FileAccess access)
        {
            FileMode fm = 0;
            if (access.HasFlag(LC_FileAccess.CreateNew))
                fm = FileMode.CreateNew;
            else if (access.HasFlag(LC_FileAccess.CreateAlways))
                fm = FileMode.Create;
            else if (access.HasFlag(LC_FileAccess.OpenAlways))
                fm = FileMode.OpenOrCreate;
            else if (access.HasFlag(LC_FileAccess.OpenAppend))
                fm = FileMode.Append;
            else
                fm = FileMode.Open;
            return fm;
        }

        private static FileAccess ToFileAccess(LC_FileAccess access)
        {
            FileAccess fa = 0;
            if (access.HasFlag(LC_FileAccess.Read))
                fa |= FileAccess.Read;
            if (access.HasFlag(LC_FileAccess.Write))
                fa |= FileAccess.Write;
            return fa;
        }


        delegate LC_FileResult fOpen_d(IntPtr* fileObject, IntPtr name, LC_FileAccess mode);
        delegate uint fTell_d(IntPtr fileObject);
        delegate LC_FileResult fSeek_d(IntPtr fileObject, uint pointer);
        delegate LC_FileResult fRead_d(IntPtr fileObject, byte* buffer, uint bytesToRead, uint* bytesReaded);
        delegate LC_FileResult fWrite_d(IntPtr fileObject, byte* buffer, uint bytesToWrite, uint* bytesWritten);
        delegate LC_FileResult fClose_d(IntPtr fileObject);
        delegate uint fSize_d(IntPtr fileObject);
        delegate LC_FileResult fTruncate_d(IntPtr fileObject);
        delegate void fOnReceive_d();

        fOpen_d fOpen;
        fTell_d fTell;
        fSeek_d fSeek;
        fRead_d fRead;
        fWrite_d fWrite;
        fClose_d fClose;
        fSize_d fSize;
        fTruncate_d fTruncate;
        fOnReceive_d fOnReceive;

        [DllImport("LEVCANlibx64", EntryPoint = "LC_Set_FileCallbacks", CharSet = CharSet.Ansi)]
        private static extern void lib_setFileCallbacks(fOpen_d fopen, fTell_d ftell, fSeek_d flseek, fRead_d fread, fWrite_d fwrite, fClose_d fclose, fTruncate_d ftruncate, fSize_d fsize, fOnReceive_d onrec);

        [DllImport("LEVCANlibx64", EntryPoint = "LC_FileServerInit", CharSet = CharSet.Ansi)]
        private static extern LC_Return lib_FileServerInit(IntPtr node);

        [DllImport("LEVCANlibx64", EntryPoint = "LC_FileServer", CharSet = CharSet.Ansi)]
        private static extern void lib_FileServer(IntPtr node, uint tick);

        LC_Node _node;
        SemaphoreSlim mutex;
        Dictionary<int, FileStream> files = new Dictionary<int, FileStream>();
        int fileIndex = 1; //intptr=0 - error
        string savePath;

        public string SavePath
        {
            get { return savePath; }
            set
            {
                if (!Directory.Exists(value))
                {
                    Directory.CreateDirectory(value);
                }
                //todo close opened files? or keep them alive sice there is no conflict?
                savePath = value;
            }
        }

        public LC_FileServer(LC_Node node)
        {
            initCallbacks();
            lib_FileServerInit(node.DescriptorPtr);
            _node = node;
            SavePath = Path.Combine(Directory.GetCurrentDirectory(), "files");

            mutex = new SemaphoreSlim(0);
            var updates = new Thread(FileServerThread);
            updates.IsBackground = true; //testing
            updates.Start();
        }

        void initCallbacks()
        {
            if (fOpen != null)
                return;

            fOpen = fileOpen;
            fTell = fileTell;
            fSeek = fileSeek;
            fRead = fileRead;
            fWrite = fileWrite;
            fClose = fileClose;
            fSize = fileSize;
            fTruncate = fileTruncate;
            fOnReceive = fileOnReceive;
            lib_setFileCallbacks(fOpen, fTell, fSeek, fRead, fWrite, fClose, fTruncate, fSize, fOnReceive);
        }

        public void SetSavePath(string path)
        {
            if (Directory.Exists(path))
                savePath = path;
            else
                throw new DirectoryNotFoundException("Path not found: " + path);
        }

        unsafe LC_FileResult fileOpen(IntPtr* fileObject, IntPtr name, LC_FileAccess mode)
        {
            LC_FileResult res = LC_FileResult.Ok;
            string file = Text8z.PtrToString(name, _node.ShortName.CodePage);
            string path = Path.GetFullPath(Path.Combine(savePath, file));
            if (!path.StartsWith(savePath, StringComparison.Ordinal))
            {
                return LC_FileResult.InvalidName;
            }

            try
            {
                //File uses stream buffer so should work pretty ok, unless your hdd is f*d up
                FileStream fs = new FileStream(path, ToFileMode(mode), ToFileAccess(mode));
                /*if (fs.Length < 10 * 1024 * 1024)
                {
                    //10mb file size, just copy to ram
                    MemoryStream ms = new MemoryStream((int)fs.Length);
                    fs.CopyTo(ms);
                    fs.Close();
                    fs = ms;
                }*/
                *fileObject = new IntPtr(fileIndex);
                fileIndex++;
                files.Add(fileObject->ToInt32(), fs);
            }
            catch (FileNotFoundException)
            {
                res = LC_FileResult.NoFile;
            }
            catch (System.Security.SecurityException)
            {
                res = LC_FileResult.Denied;
            }
            catch (DirectoryNotFoundException)
            {
                res = LC_FileResult.NoPath;
            }
            catch (UnauthorizedAccessException)
            {
                res = LC_FileResult.Denied;
            }
            catch (PathTooLongException)
            {
                res = LC_FileResult.InvalidName;
            }
            catch (IOException)
            {
                if (ToFileMode(mode) == FileMode.CreateNew)
                    res = LC_FileResult.Exist;
                else
                    res = LC_FileResult.IntErr;
            }
            catch (NotSupportedException)
            {
                res = LC_FileResult.IntErr;
            }

            return res;
        }

        uint fileTell(IntPtr fileObject)
        {
            int key = fileObject.ToInt32();
            if (files.ContainsKey(key))
            {
                return (uint)files[key].Position;
            }
            else
                return 0;
        }

        LC_FileResult fileSeek(IntPtr fileObject, uint pointer)
        {
            var res = FileOpHelper.CallFileGetResult(() =>
            {
                int key = fileObject.ToInt32();
                files[key].Seek(pointer, SeekOrigin.Begin);
            });

            return res;
        }

        LC_FileResult fileRead(IntPtr fileObject, byte* buffer, uint bytesToRead, uint* bytesReaded)
        {
            var res = FileOpHelper.CallFileGetResult(() =>
            {
                int key = fileObject.ToInt32();
                FileStream fs = files[key];
                var ums = new UnmanagedMemoryStream(buffer, bytesToRead, bytesToRead, FileAccess.ReadWrite);

                byte[] buff = new byte[bytesToRead];
                *bytesReaded = (uint)fs.Read(buff, 0, (int)bytesToRead);
                ums.Write(buff, 0, (int)*bytesReaded);
            });

            return res;
        }

        LC_FileResult fileWrite(IntPtr fileObject, byte* buffer, uint bytesToWrite, uint* bytesWritten)
        {
            var res = FileOpHelper.CallFileGetResult(() =>
            {
                int key = fileObject.ToInt32();
                FileStream fs = files[key];
                var ums = new UnmanagedMemoryStream(buffer, bytesToWrite);
                long pos = fs.Position;
                ums.CopyTo(fs);
                *bytesWritten = bytesToWrite;
            });
            return res;
        }

        LC_FileResult fileClose(IntPtr fileObject)
        {
            var res = FileOpHelper.CallFileGetResult(() =>
            {
                int key = fileObject.ToInt32();
                FileStream fs = files[key];
                fs.FlushAsync().ContinueWith((a) => { fs.Close(); });
            });

            if (res == LC_FileResult.Ok)
            {
                files.Remove(fileObject.ToInt32());
            }
            return res;
        }

        uint fileSize(IntPtr fileObject)
        {
            uint outp = 0;
            var res = FileOpHelper<uint>.CallFileGetResult(() =>
            {
                int key = fileObject.ToInt32();
                FileStream fs = files[key];
                return (uint)fs.Length;
            }, ref outp);
            return outp;
        }

        LC_FileResult fileTruncate(IntPtr fileObject)
        {
            var res = FileOpHelper.CallFileGetResult(() =>
            {
                int key = fileObject.ToInt32();
                FileStream fs = files[key];
                fs.SetLength(fs.Position);
            });
            return res;
        }

        void fileOnReceive()
        {
            mutex.Release();
        }

        private void FileServerThread()
        {
            var watch = new Stopwatch();

            watch.Start();
            while (true)
            {
                mutex.Wait(100);
                watch.Stop();
                uint elaps = (uint)watch.ElapsedMilliseconds;
                watch.Restart();
                lib_FileServer(_node.DescriptorPtr, elaps);
            }
        }
    }

    class FileOpHelper<TResult>
    {
        public static LC_FileResult CallFileGetResult(Func<TResult> func, ref TResult output)
        {
            var res = LC_FileResult.Ok;
            try
            {
                output = func();
            }
            catch (FileNotFoundException fnf)
            {
                res = LC_FileResult.NoFile;
            }
            catch (System.Security.SecurityException se)
            {
                res = LC_FileResult.Denied;
            }
            catch (DirectoryNotFoundException dnf)
            {
                res = LC_FileResult.NoPath;
            }
            catch (UnauthorizedAccessException ae)
            {
                res = LC_FileResult.Denied;
            }
            catch (PathTooLongException ptle)
            {
                res = LC_FileResult.InvalidName;
            }
            catch (IOException ioexep)
            {
                res = LC_FileResult.IntErr;
            }
            catch (NotSupportedException nse)
            {
                res = LC_FileResult.IntErr;
            }
            catch (ObjectDisposedException nse)
            {
                res = LC_FileResult.FileNotOpened;
            }
            catch (KeyNotFoundException nse)
            {
                res = LC_FileResult.FileNotOpened;
            }
            catch
            {
                res = LC_FileResult.IntErr;
            }
            return res;
        }
    }

    class FileOpHelper
    {
        public static LC_FileResult CallFileGetResult(Action func)
        {
            var res = LC_FileResult.Ok;
            try
            {
                func();
            }
            catch (FileNotFoundException fnf)
            {
                res = LC_FileResult.NoFile;
            }
            catch (System.Security.SecurityException se)
            {
                res = LC_FileResult.Denied;
            }
            catch (DirectoryNotFoundException dnf)
            {
                res = LC_FileResult.NoPath;
            }
            catch (UnauthorizedAccessException ae)
            {
                res = LC_FileResult.Denied;
            }
            catch (PathTooLongException ptle)
            {
                res = LC_FileResult.InvalidName;
            }
            catch (IOException ioexep)
            {
                res = LC_FileResult.IntErr;
            }
            catch (NotSupportedException nse)
            {
                res = LC_FileResult.IntErr;
            }
            catch (ObjectDisposedException nse)
            {
                res = LC_FileResult.FileNotOpened;
            }
            catch (KeyNotFoundException nse)
            {
                res = LC_FileResult.FileNotOpened;
            }
            catch
            {
                res = LC_FileResult.IntErr;
            }
            return res;
        }
    }
}
