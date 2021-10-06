﻿using HidSharp;
using LEVCAN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.Collections.Concurrent;
//using System.IO.Ports;

namespace LEVCANsharpTest
{
    class SerialNode
    {
        readonly int cdc_packsz = Marshal.SizeOf(typeof(CANdata));
        public int tx = 0, rx = 0, errb = 0;
        List<byte> buffer = new List<byte>();
        byte[] portbuff;
        bool portTxFull = false;
        SemaphoreSlim semaphoreST;
        public Stopwatch swGlb = new Stopwatch();
        public TimeSpan elapsed;
        public TimeSpan maxel = TimeSpan.Zero;
        public int elcntr = 1;
        bool testing = false;
        SerialStream port;
        //SerialPort port;
        BlockingCollection<CANdata> sendQueue = new BlockingCollection<CANdata>();
        LC_Node _node;

        public float AvgElapsed
        {
            get
            {
                if (elcntr > 0)
                {
                    var avg = (float)elapsed.TotalMilliseconds / elcntr;
                    elapsed = TimeSpan.Zero; elcntr = 0;
                    return avg;
                }
                return 0;
            }
        }

        public SerialNode(LC_Node node)
        {
            _node = node;
            portbuff = new byte[cdc_packsz];
            DeviceList.Local.Changed += DeviceListChanged;
            Task.Run(() => { DeviceList.Local.RaiseChanged(); });

            LC_Interface.SetFilterCallback(FilterCallback);
            LC_Interface.SetSendCallback(SendCallback);
            LC_Interface.InitQHandlers();

            var send = new Thread(SendQueueToSerial);
            send.IsBackground = true; //testing
            send.Start();
            send.Name = "Serial Node Send";

            var receive = new Thread(ReceiveFromSerial);
            receive.IsBackground = true; //testing
            receive.Start();
            receive.Name = "Serial Node Receive";
        }

        void SendQueueToSerial()
        {
            List<byte> data = new List<byte>();
            CANdata newItem;
            for (; ; )
            {
                //get all possible data without timeout
                while (sendQueue.TryTake(out newItem, 0))
                {
                    var bytes = StructHelper.StructToBytes(newItem);
                    data.AddRange(bytes);
                }
                try
                {
                    port?.Write(data.ToArray(), 0, data.Count);
                }
                catch { }
                data.Clear();
                //wait for more items long time
                if (sendQueue.TryTake(out newItem, -1))
                {
                    var bytes2 = StructHelper.StructToBytes(newItem);
                    data.AddRange(bytes2);
                }
            }
        }

        private void DeviceListChanged(object sender, DeviceListChangedEventArgs e)
        {
            if (portTxFull)
                Port_Closed(null, null);
            var listdev = DeviceList.Local.GetSerialDevices();
            if (port != null && !listdev.Contains(port.Device))
            {
                //device is gone. Why not closed??hz
                port = null;
            }
            foreach (SerialDevice dev in listdev)
            {
                try
                {
                    var stream = dev.Open();
                    string text = null;
                    text = stream.ReadLine();

                    if (text != null && text.Contains("Nucular"))
                    {
                        //Found nucular device
                        port = stream;
                        port.Closed += Port_Closed;
                       // port.ReadTimeout = Timeout.Infinite;
                        //port.BeginRead(portbuff, 0, cdc_packsz, receivedCallback, null);

                        LC_Interface.InitFilters();
                        /* var updates = new Thread(ResponceTest);
                         updates.IsBackground = true; //testing
                         updates.Start();
                         testing = true;*/
                    }
                    else
                        port.Close(); //stream.Close(); 

                }
                catch { }
            }

        }

        private void ReceiveFromSerial()
        {
            for (; ; )
            {
                int btr = cdc_packsz;
                int br = 0;
                try
                {
                    if (port != null)
                    {
                        port.ReadTimeout = 3000;
                        br = port.Read(portbuff, 0, 1);
                        if (br > 0)
                        {
                            port.ReadTimeout = 0;
                            int received = 0;
                            do
                            {
                                received = port.Read(portbuff, br, 1);
                                br += received;
                            } while (received > 0 && br < 16);
                        }
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
                catch
                {
                }

                if (br > 0)
                    buffer.AddRange(portbuff.AsSpan(0, br).ToArray());

                while (buffer.Count >= cdc_packsz)
                {
                    //DEAD + 01/00
                    if (buffer[0] == 0xAD && buffer[1] == 0xDE)
                    {
                        if (buffer[2] < 2)
                        {
                            var candata = StructHelper.BytesToStructure<CANdata>(buffer.ToArray());
                            LC_HeaderPacked pckd = new LC_HeaderPacked(candata.ID);

                            if (!testing && pckd.MsgID == 904 && pckd.EoM == 1)
                            {
                                swGlb.Stop();
                                elapsed += swGlb.Elapsed;
                                if (maxel < swGlb.Elapsed)
                                    maxel = swGlb.Elapsed;
                                elcntr++;
                            }
                            //data ready! pack ID
                            LC_Interface.lib_ReceiveHandler(_node.DescriptorPtr, candata.ID, candata.Data, candata.Length);
                            rx++;
                        }
                        else if (buffer[2] == 2)
                        {
                            if (semaphoreST != null)
                            {
                                if (semaphoreST.CurrentCount > 0)
                                {
                                    //for debug
                                }
                                else
                                    semaphoreST.Release();
                            }
                        }
                        if (buffer.Count >= cdc_packsz)
                            buffer.RemoveRange(0, cdc_packsz); //del one byte
                                                               //else
                                                               //    buffer.Clear();
                    }
                    else
                    {
                        errb++;
                        buffer.RemoveAt(0); //del one byte
                    }
                }
            }
        }

        private void Port_Closed(object sender, EventArgs e)
        {
            if (port != null)
            {
                try
                {
                    port.Close();
                    port.Dispose();
                }
                catch { }
                port = null;
            }
        }

        private LC_Return SendCallback(uint header, uint[] data, byte length)
        {
            if (port == null)
                return LC_Return.BufferFull;

            CANdata senddata = new CANdata();
            senddata.ID = header;
            senddata.Key = 0xDEAD;
            senddata.Data = data;
            senddata.Length = length;
            senddata.Command = 0;
            LC_HeaderPacked pckd = new LC_HeaderPacked(header);

            if (!testing && pckd.MsgID == 904)
            {
                swGlb.Restart();
            }
            tx++;
            try
            {
                sendQueue.TryAdd(senddata);
            }
            catch
            {
                portTxFull = true;
                return LC_Return.BufferFull;
            }

            if (portTxFull)
            {
                portTxFull = false;
            }

            return LC_Return.Ok;
        }

        Mutex once = new Mutex(false);
        public void ResponceTest()
        {
            while (true)
            {
                if (port != null && once.WaitOne(0))
                {
                    CANdata senddata = new CANdata();
                    senddata.ID = 0xBEEF;
                    senddata.Key = 0xDEAD;
                    senddata.Data = new uint[2];
                    senddata.Length = 0;
                    senddata.Command = 2; //test

                    Stopwatch sw = new Stopwatch();
                    semaphoreST = new SemaphoreSlim(0);
                    sw.Start();

                    sendQueue.TryAdd(senddata);

                    semaphoreST.Wait(2000);
                    sw.Stop();
                    once.ReleaseMutex();

                    elapsed += sw.Elapsed;

                    if (maxel < sw.Elapsed)
                        maxel = sw.Elapsed;
                    elcntr++;
                }
            }
        }

        private LC_Return FilterCallback(uint reg, uint mask, byte index)
        {
            CANdata senddata = new CANdata();
            senddata.ID = reg;
            senddata.Key = 0xDEAD;
            senddata.Command = 1; //filter set
            senddata.Data = new uint[2];
            senddata.Data[0] = mask;
            senddata.Data[1] = index;

            sendQueue.TryAdd(senddata);
            return LC_Return.Ok;

        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        struct CANdata
        {
            public ushort Key;
            public byte Command;
            public byte Length;
            public uint ID;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public uint[] Data;
        }
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
