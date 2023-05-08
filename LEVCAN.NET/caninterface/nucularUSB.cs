using HidSharp;
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
using System.IO;
//using System.IO.Ports;

namespace LEVCAN
{

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    struct usbHeader_t
    {
        public byte TxNumber;
        public byte Button;
        public byte Enable_FB;
        public short ADC_raw;
        public short FreeQueue;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    struct usbStatusFrame_t
    {
        public byte Divider;
        public byte Number;
        public byte Button;
        public byte CAN_EN_FB;
        public ushort ADC_RAW;
        public ushort FreeCANframes;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public byte[] reserved;
        public byte CRC_ITU;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    struct usbFrame_t
    {
        public byte Divider;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 14)]
        public byte[] Data;
        public byte CRC_ITU;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    struct usbCanFrame_t
    {
        public byte Divider;
        public byte Number;
        public byte DLC;
        public uint EXID_RTR; //RTR 1<<30, IDtype11b 1<<31
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public uint[] Data;
        public byte CRC_ITU;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    struct usbFrameFilter29b_t
    {
        public byte Divider;
        public byte Command;
        public byte Bank;
        public byte FilterMode;
        public uint ID;
        public uint Mask;
        public byte reserved1;
        public byte reserved2;
        public byte reserved3;
        public byte CRC_ITU;
    }

    enum usbFPos : int
    {
        Divider = 0,
        Data = 1,
        CRC_ITU = 15,
    }

    enum usbFrameID : byte
    {
        CAN = 0xA5,
        Command = 0xA6,
        USB = 0xA7
    }

    enum usbFilterInit : byte
    {
        Disable = 0,
        Mask11b,
        Array11b,
        Mask29b,
        Array29b
    }

    enum usbCommand : byte
    {
        Reset = 0,
        LED = 0x1,
        InitCAN = 0x10,
        Filters = 0x11,
        ResetCANCounter = 0x30,
        ResetUSBCounter = 0x31,
        GetStatus = 0x40
    }

    public class NucularUSB2CAN : Icanbus
    {
        readonly int cdc_packsz = Marshal.SizeOf(typeof(usbFrame_t));
        public int txcounter = 0, rxcounter = 0, errors = 0;
        List<byte> buffer = new List<byte>();
        byte[] portbuff;
        bool portTxFull = false;

        public Stopwatch swGlb = new Stopwatch();
        public TimeSpan elapsed;
        public TimeSpan maxel = TimeSpan.Zero;
        public int elcntr = 1;
        bool testing = false;
        SerialStream? serialStream;
        usbStatusFrame_t usbStatus;
        //SerialPort port;
        BlockingCollection<byte[]> sendQueue = new BlockingCollection<byte[]>();
        LC_Node _node;
        Thread sendThread;
        Thread receiveThread;
        byte sentIndex;
        CancellationTokenSource cts;

        public event EventHandler OnDisconnected;
        public event EventHandler OnConnected;

        public int TXcounter { get { return txcounter; } set { txcounter = value; } }
        public int RXcounter { get { return rxcounter; } set { rxcounter = value; } }
        public int Errors { get { return errors; } set { errors = value; } }
        public TimeSpan MaxRequestDelay { get { return maxel; } set { maxel = value; } }

        public NucularUSB2CAN(LC_Node node)
        {
            _node = node;
            portbuff = new byte[cdc_packsz];
        }

        public void Open()
        {
            DeviceList.Local.Changed += DeviceListChanged;
            Task.Run(() => { DeviceList.Local.RaiseChanged(); });

            LC_Interface.SetFilterCallback(FilterCallback);
            LC_Interface.SetSendCallback(SendCallback);
            LC_Interface.InitQHandlers();

            cts = new CancellationTokenSource();

            sendThread = new Thread(SendQueueToSerial);
            sendThread.IsBackground = true;
            sendThread.Start(cts.Token);
            sendThread.Name = "Serial Node Send";

            receiveThread = new Thread(ReceiveFromSerial);
            receiveThread.IsBackground = true;
            receiveThread.Start(cts.Token);
            receiveThread.Name = "Serial Node Receive";
        }

        public void Close()
        {
            cts.Cancel();
            LC_Interface.SetFilterCallback(null);
            LC_Interface.SetSendCallback(null);
            DeviceList.Local.Changed -= DeviceListChanged;
        }

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

        public string Status
        {
            get
            {
                if (serialStream != null)
                    return serialStream.Device.GetFileSystemName();
                else
                    return null;
            }
        }

        void SendQueueToSerial(object otoken)
        {
            CancellationToken token = (CancellationToken)otoken;
            List<byte> data = new List<byte>();
            byte[] newItem;
            for (; token.IsCancellationRequested == false;)
            {
                //get all possible data without timeout
                while (sendQueue.TryTake(out newItem, 0) && data.Count < 512)
                {
                    //Indexate CAN frames
                    if (newItem[0] == (byte)usbFrameID.CAN && newItem[1] == 0xDA)
                        newItem[1] = sentIndex++;

                    newItem[15] = CalculatorCRC.Calculate_CRC8_ITU(newItem, 15);
                    data.AddRange(newItem);
                }
                try
                {
                    if (usbStatus.FreeCANframes < 200)
                    {
                        Task.Delay(10);
                    }
                    serialStream?.Write(data.ToArray(), 0, data.Count);
                }
                catch { }
                data.Clear();
                //wait for more items long time
                if (sendQueue.TryTake(out newItem, -1))
                {
                    //Indexate CAN frames
                    if (newItem[0] == (byte)usbFrameID.CAN && newItem[1] == 0xDA)
                        newItem[1] = sentIndex++;

                    newItem[15] = CalculatorCRC.Calculate_CRC8_ITU(newItem, 15);
                    data.AddRange(newItem);
                }
            }
        }

        private void DeviceListChanged(object sender, DeviceListChangedEventArgs e)
        {
            if (portTxFull)
                Port_Closed(this, null);
            var listdev = DeviceList.Local.GetSerialDevices();
            if (serialStream != null)
            {
                if (!listdev.Contains(serialStream.Device))
                {
                    //device is gone. Why not closed??hz
                    Port_Closed(this, null);
                }

                else
                    return;
            }

            foreach (SerialDevice dev in listdev)
            {
                try
                {
                    var stream = usbDongleInit(dev);

                    int readed = 0;
                    byte[] redFrame = new byte[16];
                    try
                    {
                        while (readed < redFrame.Length)
                            readed += stream.Read(redFrame, readed, redFrame.Length - readed);
                    }
                    catch { }

                    //shitty way to check our dongle - crc calc
                    usbFrame_t fr = CastingHelper.CastToStruct<usbFrame_t>(redFrame);
                    bool crcchek = CalculatorCRC.Calculate_CRC8_ITU(redFrame, 15) == fr.CRC_ITU;

                    if (readed > 0 && crcchek)
                    {
                        //Found nucular device
                        sentIndex = 0;
                        serialStream = stream;
                        serialStream.Closed += Port_Closed;

                        LC_Interface.InitFilters();
                        OnConnected?.Invoke(this, EventArgs.Empty);
                    }
                    else
                        stream.Close();

                }
                catch { }
            }

        }

        private void ReceiveFromSerial(object otoken)
        {
            CancellationToken token = (CancellationToken)otoken;
            //Very special byte-by-byte read from COM port
            for (; token.IsCancellationRequested == false;)
            {
                int btr = cdc_packsz;
                int br = 0;
                try
                {
                    if (serialStream != null)
                    {
                        serialStream.ReadTimeout = 3000;
                        br = serialStream.Read(portbuff, 0, 1);
                        if (br > 0)
                        {
                            serialStream.ReadTimeout = 0;
                            int received = 0;
                            do
                            {
                                received = serialStream.Read(portbuff, br, 1);
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
                    if (buffer[0] >= 0xA5 && buffer[0] <= 0xA7)
                    {
                        if (buffer[0] == (byte)usbFrameID.CAN)//CAN frame
                        {
                            var candata = StructHelper.BytesToStructure<usbCanFrame_t>(buffer.ToArray());
                            uint convertID = (candata.EXID_RTR) & 0x1FFFFFFF;
                            convertID |= (candata.EXID_RTR & (1 << 30)) >> 1; //shift RTR
                            LC_HeaderPacked pckd = new LC_HeaderPacked(convertID);

                            if (!testing && pckd.MsgID == 904 && pckd.EoM == 1)
                            {
                                swGlb.Stop();
                                elapsed += swGlb.Elapsed;
                                if (maxel < swGlb.Elapsed)
                                    maxel = swGlb.Elapsed;
                                elcntr++;
                            }
                            //data ready! pack ID
                            LC_Interface.lib_ReceiveHandler(_node.DescriptorPtr, convertID, candata.Data, candata.DLC);
                            rxcounter++;
                        }
                        else if (buffer[0] == (byte)usbFrameID.USB)//USB frame
                        {
                            usbStatus = CastingHelper.CastToStruct<usbStatusFrame_t>(buffer.ToArray());
                            usbStatus.FreeCANframes = (ushort)(usbStatus.FreeCANframes >> 8);
                        }
                        buffer.RemoveRange(0, cdc_packsz); //del one byte
                    }
                    else
                    {
                        errors++;
                        buffer.RemoveAt(0); //del one byte
                    }
                }
            }
        }

        private void Port_Closed(object? sender, EventArgs e)
        {
            if (serialStream != null)
            {
                try
                {
                    serialStream.Close();
                    //port.Dispose();
                }
                catch { }
                serialStream = null;
                OnDisconnected?.Invoke(this, EventArgs.Empty);
            }
        }
        private LC_Return SendCallback(uint header, uint[] data, byte length)
        {
            if (serialStream == null)
                return LC_Return.BufferFull;
            //idType =0 for 29b
            uint convertID = header & 0x1FFFFFFF;
            convertID |= (header & (1 << 29)) << 1; //shift RTR

            usbCanFrame_t senddata = new usbCanFrame_t();
            senddata.Divider = (byte)usbFrameID.CAN;
            senddata.Number = 0xDA;// sentIndex++;
            senddata.DLC = length;
            senddata.EXID_RTR = convertID;
            senddata.Data = data;
            LC_HeaderPacked pckd = new LC_HeaderPacked(header);

            if (!testing && pckd.MsgID == 904)
            {
                swGlb.Restart();
            }
            txcounter++;
            try
            {
                var bytes = CastingHelper.CastToArray(senddata);
                sendQueue.TryAdd(bytes);
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

        private unsafe LC_Return FilterCallback(uint reg, uint mask, byte index)
        {
            uint regSTM32 = 0, maskSTM32;
            //regSTM32 |= 1 << 2; //extension ID
            maskSTM32 = regSTM32; //0x1FFFFFFF -29b

            /*
            struct {
                //index 29bit:
                uint32_t Source : 7;
                uint32_t Target : 7;
                uint32_t MsgID : 10;
                uint32_t EoM : 1;
                uint32_t Parity : 1;
                uint32_t RTS_CTS : 1;
                uint32_t Priority : 2;
                //RTR bit:
                uint32_t Request : 1;
            } ;*/

            //copy 29b EXID
            regSTM32 |= (reg & 0x1FFFFFFF)/* << 3*/;
            maskSTM32 |= (mask & 0x1FFFFFFF) /*<< 3*/;

            usbFrameFilter29b_t senddata = new usbFrameFilter29b_t();
            senddata.Divider = (byte)usbFrameID.Command;
            senddata.Command = (byte)usbCommand.Filters;//filter
            senddata.Bank = index;//bank index
            senddata.FilterMode = 0x3;//29 bit, mask
            senddata.ID = regSTM32;
            senddata.Mask = maskSTM32;

            var filter = CastingHelper.CastToArray(senddata);
            var res = sendQueue.TryAdd(filter);
            return LC_Return.Ok;

        }

        public void SetDefaultPort(string port)
        {
            if (serialStream == null)
            {
                var deviceCOM = DeviceList.Local.GetSerialDeviceOrNull(port);
                if (deviceCOM != null)
                {
                    serialStream = usbDongleInit(deviceCOM);
                    serialStream.Closed += Port_Closed;

                    LC_Interface.InitFilters();
                    OnConnected?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        SerialStream usbDongleInit(SerialDevice dev)
        {
            var stream = dev.Open();

            //read stuff 3 times to find com
            byte[] frame = new byte[16];
            frame[(int)usbFPos.Divider] = (byte)usbFrameID.Command;
            frame[(int)usbFPos.Data] = (byte)usbCommand.LED;
            frame[(int)usbFPos.CRC_ITU] = CalculatorCRC.Calculate_CRC8_ITU(frame, 15);
            stream.Write(frame, 0, frame.Length);

            frame[(int)usbFPos.Data] = (byte)usbCommand.GetStatus;
            frame[(int)usbFPos.CRC_ITU] = CalculatorCRC.Calculate_CRC8_ITU(frame, 15);
            stream.Write(frame, 0, frame.Length);

            //Init can bus
            int index = (int)usbFPos.Data;
            frame[index++] = (byte)usbCommand.InitCAN;
            frame[index++] = 0x40; //000F4240 = 1mhz
            frame[index++] = 0x42;
            frame[index++] = 0x0F;
            frame[index++] = 0x00;
            frame[index++] = 0x00;//loopback
            frame[index++] = 0x00;//silent
            frame[(int)usbFPos.CRC_ITU] = CalculatorCRC.Calculate_CRC8_ITU(frame, 15);
            stream.Write(frame, 0, frame.Length);

            frame[(int)usbFPos.Data] = (byte)usbCommand.ResetCANCounter;
            frame[(int)usbFPos.CRC_ITU] = CalculatorCRC.Calculate_CRC8_ITU(frame, 15);
            stream.Write(frame, 0, frame.Length);

            return stream;
        }
    }
}
