using HidSharp;
using LEVCAN;
using System;
using System.Text;

namespace LEVCAN_Configurator
{
    internal class PrinterCOM
    {
        public static void Print(string text, string port)
        {
            SerialDevice device;
            if (DeviceList.Local.TryGetSerialDevice(out device, port))
            {
                DeviceStream stream;
                if (device.TryOpen(out stream))
                {
                    var arraybyte = Encoding.ASCII.GetBytes(text);
                    stream.Write(arraybyte);
                    stream.Close();
                }
            }
        }
    }
}
