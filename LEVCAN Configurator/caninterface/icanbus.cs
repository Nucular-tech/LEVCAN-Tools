using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LEVCANsharpTest
{
    interface Icanbus
    {
        int TXcounter { get; set; }
        int RXcounter { get; set; }
        int Errors { get; set; }
        string Status { get; }
        TimeSpan MaxRequestDelay { get; set; }

        event EventHandler OnDisconnected;
        event EventHandler OnConnected;

        void Open();
        void Close();
    }


    class StructHelper
    {

        public static T BytesToStructure<T>(byte[] bytes, int offset)
        {

            int size = Marshal.SizeOf(typeof(T));
            if (bytes.Length - offset < size)
                throw new Exception("Invalid parameter");

            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(bytes, offset, ptr, size);
                return (T)Marshal.PtrToStructure(ptr, typeof(T));
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        public static T BytesToStructure<T>(byte[] bytes)
        {
            return BytesToStructure<T>(bytes, 0);
        }

        public static byte[] StructToBytes(object structdata)
        {

            int size = Marshal.SizeOf(structdata);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(structdata, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }

    }
}
