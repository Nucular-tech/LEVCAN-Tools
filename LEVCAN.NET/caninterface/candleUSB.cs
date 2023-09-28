using LEVCAN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.IO;
using Candle;
using System.Threading.Channels;
using System.Collections;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Timers;
using Timer = System.Timers.Timer;
//using System.IO.Ports;

namespace LEVCAN
{
    public class CandleUSB : Icanbus
    {
        public int txcounter = 0, rxcounter = 0, errors = 0;

        LC_Node _node;
        byte sentIndex;

        public event EventHandler OnDisconnected;
        public event EventHandler OnConnected;

        public int TXcounter { get { return txcounter; } set { txcounter = value; } }
        public int RXcounter { get { return rxcounter; } set { rxcounter = value; } }
        public int Errors { get { return errors; } set { errors = value; } }
        public TimeSpan MaxRequestDelay { get { return TimeSpan.Zero; } set { } }

        byte[] idFilter;
        Device canDevice;
        Candle.Channel canChannel;

        Timer deviceDelayTim = new Timer(TimeSpan.FromSeconds(1));

        public CandleUSB(LC_Node node)
        {
            idFilter = new byte[3];
            for (int i = 0; i < idFilter.Length; i++)
            {
                idFilter[i] = (byte)LC_Address.Broadcast;
            }

            _node = node;
            deviceDelayTim.Elapsed += delayTim_Elapsed;
        }

        private void delayTim_Elapsed(object? sender, ElapsedEventArgs e)
        {
            if (canChannel != null)
                return;
            Close();
            Task.Delay(100);
            Open();
        }

        public void Open()
        {
            LC_Interface.SetFilterCallback(FilterCallback);
            LC_Interface.SetSendCallback(SendCallback);
            LC_Interface.InitQHandlers();

            var devices = Device.ListDevices();
            if (devices.Count > 0)
            {
                canDevice = devices[0];
                canDevice.Open();
                canChannel = canDevice.Channels[0];
                canChannel.Start(1000000);
                canChannel.OnReceive += CanChannel_OnReceive;
                LC_Interface.ConfigureFilters(_node);
            }
            else
                deviceDelayTim.Start();

        }

        private void CanChannel_OnReceive(Frame frame)
        {
            LC_HeaderPacked headerPacked = new LC_HeaderPacked(frame.Identifier);

            if (frame.RTR)
                headerPacked.Request = 1;

            if (frame.Extended == false)
                return;
            if (frame.Error)
                return;
            if (frame.Echo)
                return;
            //filter
            bool filterPassed = false;
            for (int i = 0; i < idFilter.Length; i++)
            {
                if (idFilter[i] == headerPacked.Target)
                {
                    filterPassed = true;
                    break;
                }
            }
            if (filterPassed)
            {
                rxcounter++;
                uint[] data = new uint[2];
                Buffer.BlockCopy(frame.Data, 0, data, 0, frame.Data.Length);
                LC_Interface.lib_ReceiveHandler(_node.DescriptorPtr, headerPacked.ToUint, data, (byte)frame.Data.Length);
            }
        }

        public void Close()
        {
            LC_Interface.SetFilterCallback(null);
            LC_Interface.SetSendCallback(null);
            try
            {
                canChannel?.Stop();
                canDevice?.Close();
            }
            catch { }
        }

        public string Status
        {
            get
            {
                if (canChannel != null)
                    return "CandleLight USB";
                else
                    return null;
            }
        }

        private LC_Return SendCallback(uint header, uint[] data, byte length)
        {
            if (canChannel == null)
                return LC_Return.BufferFull;
            //idType =0 for 29b
            uint convertID = header & 0x1FFFFFFF;
            bool rtr = (header & (1 << 29)) > 0; //shift RTR
            byte[] dataAsB = new byte[length];
            if (data != null)
                Buffer.BlockCopy(data, 0, dataAsB, 0, length);

            Frame senddata = new Frame();
            senddata.Extended = true;
            senddata.RTR = rtr;
            senddata.Identifier = convertID;
            senddata.Data = dataAsB;

            txcounter++;
            try
            {
                canChannel.Send(senddata);
            }
            catch
            {
                return LC_Return.BufferFull;
            }

            return LC_Return.Ok;
        }


        Mutex once = new Mutex(false);

        private LC_Return FilterCallback(uint reg, uint mask, byte index)
        {
            LC_HeaderPacked headerPacked = new LC_HeaderPacked(reg);
            //we need only ID's for filtering
            if (index < idFilter.Length)
            {
                idFilter[index] = headerPacked.Target;
                return LC_Return.Ok;
            }
            return LC_Return.OutOfRange;
        }

        public void SetDefaultPort(string port)
        {

        }

    }
}
