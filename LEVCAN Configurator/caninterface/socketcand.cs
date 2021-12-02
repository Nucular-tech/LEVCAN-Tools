using LEVCAN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;

namespace LEVCANsharpTest
{
    class socketcand : Icanbus
    {
        LC_Node _node;
        int txcounter, rxcounter, errors;
        UdpClient discoveryListener;

        TcpClient tcpClient;
        NetworkStream tcpStream;
        Thread receiveThread;
        byte[] idFilter;
        public event EventHandler OnDisconnected;

        public int TXcounter { get { return txcounter; } set { txcounter = value; } }
        public int RXcounter { get { return rxcounter; } set { rxcounter = value; } }
        public int Errors { get { return errors; } set { errors = value; } }
        public TimeSpan MaxRequestDelay { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public socketcand(LC_Node node)
        {
            idFilter = new byte[3];
            for (int i = 0; i < 3; i++)
            {
                idFilter[i] = (byte)LC_Address.Broadcast;
            }
            _node = node;
            discoveryListener = new UdpClient(42000);
        }

        void OnUdpDiscovery(object data)
        {
            Task<UdpReceiveResult> res = (Task<UdpReceiveResult>)data;
            UdpReceiveResult resUdp = res.Result;
            //server = resUdp.RemoteEndPoint;
            string discovery = ASCIIEncoding.ASCII.GetString(resUdp.Buffer);
            Debug.WriteLine("UDP: " + discovery);

            Regex regex = new Regex("<URL>can://([0-9.]+):([0-9]+)</URL><Bus name=\"([^ \"]*)\"/>");
            var match = regex.Match(discovery);
            if (match != null && match.Groups.Count == 4)
            {
                string busname = match.Groups[3].Value;
                tcpClient = new TcpClient(match.Groups[1].Value, int.Parse(match.Groups[2].Value));
                tcpStream = tcpClient.GetStream();
                var resp = tcpStream.ReadText();
                if (resp == "< hi >")
                {
                    //server found, try open its can bus name
                    tcpStream.WriteText($"< open {busname} >");
                    resp = tcpStream.ReadText();
                    if (resp == "< ok >")
                    {
                        //switch to raw mode of can messages
                        tcpStream.WriteText("< rawmode >");
                        resp = tcpStream.ReadText();

                        receiveThread = new Thread(ReceiveFromSocket);
                        receiveThread.IsBackground = true; //testing
                        receiveThread.Start();
                        receiveThread.Name = "Serial Node Receive";
                        return;
                    }
                }
            }
            if (tcpClient != null)
            {
                tcpClient.Close();
                tcpClient = null;
            }
            if (tcpStream != null)
                tcpStream = null;
            //try open socket again
            Open();

        }

        async void ReceiveFromSocket()
        {
            string line = "";
            byte[] bufferrx = new byte[256];
            int rxb = 0;
            int endinx = -1; //end index
            while (true)
            {
                try
                {
                    rxb = await tcpStream.ReadAsync(bufferrx, 0, bufferrx.Length);
                    line += ASCIIEncoding.ASCII.GetString(bufferrx, 0, rxb);

                    if ((endinx = line.IndexOf('>')) >= 0)
                    {
                        string command = line.Substring(0, endinx + 1).Trim();
                        ProcessCommand(command);
                        line = line.Remove(0, endinx + 1).Trim();
                    }
                    //this.processOutput(line);
                }
                catch { break; }
            }
            Thread.CurrentThread.Abort();
        }

        void ProcessCommand(string oneCommand)
        {
            bool frame = false, rtr = false;
            if (oneCommand.StartsWith("< frame"))
            {
                frame = true;
            }
            else if (oneCommand.StartsWith("< rtr"))
            {
                rtr = true;
            }

            if (frame == false && rtr == false)
                return;//unknown frame

            var splitted = oneCommand.Split(' ');
            if (splitted.Length < 3)
                return; //some error in frame

            //0  1    2   3         4        5  
            //< frame 123 23.424242 11223344 >
            //< rtr 123 23.424242 4 >
            LC_HeaderPacked headerPacked = new LC_HeaderPacked(uint.Parse(splitted[2], System.Globalization.NumberStyles.HexNumber));
            uint[] data = new uint[2];
            byte dlength = 0;
            if (rtr == false)
            {
                dlength = (byte)(splitted[4].Length / 2);

                string hex = splitted[4];
                //data ready! pack ID
                for (int i = 0; i < hex.Length >> 1; ++i)
                {
                    data[i / 4] |= (uint)((SocketHandleHelper.GetHexVal(hex[i << 1]) << 4) + (SocketHandleHelper.GetHexVal(hex[(i << 1) + 1]))) << ((i % 4) * 8);
                }
            }
            else
            {
                headerPacked.Request = 1;
            }
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
                LC_Interface.lib_ReceiveHandler(_node.DescriptorPtr, headerPacked.ToUint, data, dlength);
            }
        }

        private LC_Return SendCallback(uint header, uint[] data, byte length)
        {
            LC_HeaderPacked headerPacked = new LC_HeaderPacked(header);
            if (headerPacked.Request == 0)
            {
                var b1 = BitConverter.GetBytes(data[0]);
                var b2 = BitConverter.GetBytes(data[1]);
                string texttosend = $"< send {headerPacked.ToUint.ToString("X8")} {length.ToString()}";

                int blen = length;
                for (int i = 0; i < 4 && i < blen; i++)
                {
                    texttosend += $" {b1[i].ToString("X2")}";
                }
                blen -= 4;
                for (int i = 0; i < 4 && i < blen; i++)
                {
                    texttosend += $" {b2[i].ToString("X2")}";
                }
                texttosend += " >";
                tcpStream.WriteText(texttosend);
            }
            else
            {
                headerPacked.Request = 0;
                string texttosend = $"< sendrtr {headerPacked.ToUint.ToString("X8")} 0 >";
                tcpStream.WriteText(texttosend);
            }
            txcounter++;
            return LC_Return.Ok;
        }

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

        public void Open()
        {
            Task udpTask = discoveryListener.ReceiveAsync();
            udpTask.ContinueWith(OnUdpDiscovery);

            LC_Interface.SetFilterCallback(FilterCallback);
            LC_Interface.SetSendCallback(SendCallback);
            LC_Interface.InitQHandlers();
            LC_Interface.ConfigureFilters(_node);
        }

        public void Close()
        {
            LC_Interface.SetFilterCallback(null);
            LC_Interface.SetSendCallback(null);
            if (tcpClient != null)
            {
                tcpClient.GetStream().Close();
                tcpClient.Close();
                tcpClient = null;
            }
            if (tcpStream != null)
                tcpStream = null;
            if (receiveThread != null)
                receiveThread.Abort();
            if (discoveryListener != null)
                discoveryListener.Close();
        }
    }

    public static class SocketHandleHelper
    {
        public static void WriteText(this NetworkStream target, string text)
        {
            if (target == null) return;
            var bytes = ASCIIEncoding.ASCII.GetBytes(text);
            target.Write(bytes, 0, bytes.Length);
        }

        public static string ReadText(this NetworkStream target)
        {
            if (target == null) return null;
            byte[] buff = new byte[512];
            int size = target.Read(buff, 0, 512);
            return ASCIIEncoding.ASCII.GetString(buff, 0, size);
        }

        public static byte[] StringToByteArray(this string hex)
        {
            if (hex.Length % 2 == 1)
                throw new Exception("The binary key cannot have an odd number of digits");

            byte[] arr = new byte[hex.Length >> 1];

            for (int i = 0; i < hex.Length >> 1; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }

            return arr;
        }

        public static int GetHexVal(char hex)
        {
            int val = (int)hex;
            //For uppercase A-F letters:
            //return val - (val < 58 ? 48 : 55);
            //For lowercase a-f letters:
            //return val - (val < 58 ? 48 : 87);
            //Or the two combined, but a bit slower:
            return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }
    }
}
