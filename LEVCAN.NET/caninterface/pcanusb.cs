using LEVCAN;
using Peak.Can.Basic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;
using TPCANHandle = System.UInt16;

namespace LEVCAN
{
    public class Pcanusb : Icanbus
    {
        LC_Node _node;
        int txcounter, rxcounter, errors;

        Thread receiveThread;
        byte[] idFilter;
        private System.Threading.AutoResetEvent m_ReceiveEvent;
        private TPCANHandle m_PcanHandle;

        public event EventHandler OnDisconnected;
        public event EventHandler OnConnected;

        public int TXcounter { get { return txcounter; } set { txcounter = value; } }
        public int RXcounter { get { return rxcounter; } set { rxcounter = value; } }
        public int Errors { get { return errors; } set { errors = value; } }
        public TimeSpan MaxRequestDelay { get { return TimeSpan.Zero; } set { } }

        //reset busheavy by timer
        Timer heavyTim = new Timer(TimeSpan.FromSeconds(1));

        public Pcanusb(LC_Node node)
        {
            idFilter = new byte[3];
            for (int i = 0; i < idFilter.Length; i++)
            {
                idFilter[i] = (byte)LC_Address.Broadcast;
            }
            _node = node;
            m_ReceiveEvent = new System.Threading.AutoResetEvent(false);
            m_PcanHandle = PCANBasic.PCAN_USBBUS1;
            heavyTim.Elapsed += HeavyTim_Elapsed;
        }

        private void HeavyTim_Elapsed(object? sender, ElapsedEventArgs e)
        {
            if (PCANBasic.GetStatus(m_PcanHandle) != TPCANStatus.PCAN_ERROR_BUSHEAVY)
                return;
            //quick reset
            Close();
            Task.Delay(100);
            Open();
        }

        public string Status
        {
            get
            {
                var state = PCANBasic.GetStatus(m_PcanHandle);
                switch (state)
                {
                    default:
                        return null;
                    case TPCANStatus.PCAN_ERROR_INITIALIZE:
                    case TPCANStatus.PCAN_ERROR_BUSHEAVY:
                    case TPCANStatus.PCAN_ERROR_BUSOFF:
                        return "PCAN USB (bus off)";
                    case TPCANStatus.PCAN_ERROR_OK:
                        return "PCAN USB";
                }
            }
        }

        private void CANReadThreadFunc()
        {
            UInt32 iBuffer;
            TPCANStatus stsResult;

            iBuffer = Convert.ToUInt32(m_ReceiveEvent.SafeWaitHandle.DangerousGetHandle().ToInt32());
            // Sets the handle of the Receive-Event.
            stsResult = PCANBasic.SetValue(m_PcanHandle, TPCANParameter.PCAN_RECEIVE_EVENT, ref iBuffer, sizeof(UInt32));
            iBuffer = PCANBasic.PCAN_PARAMETER_ON;
            stsResult |= PCANBasic.SetValue(m_PcanHandle, TPCANParameter.PCAN_BUSOFF_AUTORESET, ref iBuffer, sizeof(UInt32));

            if (stsResult != TPCANStatus.PCAN_ERROR_OK)
            {
                return;
            }

            while (receiveThread != null)
            {
                // Waiting for Receive-Event
                // 
                if (m_ReceiveEvent.WaitOne(50))
                {
                    // We read at least one time the queue looking for messages.
                    // If a message is found, we look again trying to find more.
                    // If the queue is empty or an error occurr, we get out from
                    // the dowhile statement.
                    //			
                    do
                    {
                        stsResult = ReadMessage();
                        if (stsResult == TPCANStatus.PCAN_ERROR_ILLOPERATION)
                            break;
                        //receiving data but bus stuck? reset!
                        if (PCANBasic.GetStatus(m_PcanHandle) == TPCANStatus.PCAN_ERROR_BUSHEAVY)
                            heavyTim.Start();

                    } while ((!Convert.ToBoolean(stsResult & TPCANStatus.PCAN_ERROR_QRCVEMPTY)));
                }
            }
        }

        private TPCANStatus ReadMessage()
        {
            TPCANMsg CANMsg;
            TPCANTimestamp CANTimeStamp;
            TPCANStatus stsResult;

            stsResult = PCANBasic.Read(m_PcanHandle, out CANMsg, out CANTimeStamp);
            if (stsResult != TPCANStatus.PCAN_ERROR_QRCVEMPTY)
            {

                LC_HeaderPacked headerPacked = new LC_HeaderPacked(CANMsg.ID);
                // We process the received message
                if (CANMsg.MSGTYPE.HasFlag(TPCANMessageType.PCAN_MESSAGE_RTR))
                    headerPacked.Request = 1;
                if (!CANMsg.MSGTYPE.HasFlag(TPCANMessageType.PCAN_MESSAGE_EXTENDED))
                    return stsResult;
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
                    LC_Interface.lib_ReceiveHandler(_node.DescriptorPtr, headerPacked.ToUint, ByteToUintArr(CANMsg.DATA), CANMsg.LEN);
                }

            }
            return stsResult;
        }

        uint[] ByteToUintArr(byte[] indata)
        {
            uint[] data = new uint[2];
            //data ready! pack ID
            for (int i = 0; i < 8; ++i)
            {
                data[i / 4] |= ((uint)indata[i]) << ((i % 4) * 8);
            }
            return data;
        }


        private LC_Return SendCallback(uint header, uint[] data, byte length)
        {
            LC_HeaderPacked headerPacked = new LC_HeaderPacked(header);
            TPCANMsg CANMsg = new TPCANMsg();
            CANMsg.DATA = new byte[8];
            CANMsg.MSGTYPE = TPCANMessageType.PCAN_MESSAGE_EXTENDED;

            if (headerPacked.Request != 0)
            {
                headerPacked.Request = 0;
                CANMsg.MSGTYPE |= TPCANMessageType.PCAN_MESSAGE_RTR;
            }
            CANMsg.ID = headerPacked.ToUint;
            CANMsg.LEN = length;
            if (data != null)
            {
                CANMsg.DATA[0] = (byte)((data[0] >> 0 * 8) & 0xFF);
                CANMsg.DATA[1] = (byte)((data[0] >> 1 * 8) & 0xFF);
                CANMsg.DATA[2] = (byte)((data[0] >> 2 * 8) & 0xFF);
                CANMsg.DATA[3] = (byte)((data[0] >> 3 * 8) & 0xFF);

                CANMsg.DATA[4] = (byte)((data[1] >> 0 * 8) & 0xFF);
                CANMsg.DATA[5] = (byte)((data[1] >> 1 * 8) & 0xFF);
                CANMsg.DATA[6] = (byte)((data[1] >> 2 * 8) & 0xFF);
                CANMsg.DATA[7] = (byte)((data[1] >> 3 * 8) & 0xFF);
            }
            var busstatus = PCANBasic.GetStatus(m_PcanHandle);
            if (busstatus == TPCANStatus.PCAN_ERROR_OK)
            {
                var sts = PCANBasic.Write(m_PcanHandle, ref CANMsg);

                if (sts != TPCANStatus.PCAN_ERROR_OK)
                {
                    errors++;
                    StringBuilder strTemp;
                    strTemp = new StringBuilder(256);
                    PCANBasic.GetErrorText(sts, 0, strTemp);
                }
                else
                    txcounter++;
                return LC_Return.Ok;
            }

            return LC_Return.BufferFull;
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

            LC_Interface.SetFilterCallback(FilterCallback);
            LC_Interface.SetSendCallback(SendCallback);
            LC_Interface.InitQHandlers();
            LC_Interface.ConfigureFilters(_node);

            TPCANStatus stsResult;
            stsResult = PCANBasic.Initialize(m_PcanHandle, TPCANBaudrate.PCAN_BAUD_1M);
            if (stsResult != TPCANStatus.PCAN_ERROR_OK)
                return;

            if (receiveThread == null)
            {
                System.Threading.ThreadStart threadDelegate = new System.Threading.ThreadStart(this.CANReadThreadFunc);
                receiveThread = new System.Threading.Thread(threadDelegate);
                receiveThread.IsBackground = true;
                receiveThread.Start();
            }

            OnConnected?.Invoke(this, EventArgs.Empty);
        }

        public void Close()
        {
            LC_Interface.SetFilterCallback(null);
            LC_Interface.SetSendCallback(null);
            receiveThread = null;

            PCANBasic.Uninitialize(m_PcanHandle);
            OnDisconnected?.Invoke(this, EventArgs.Empty);
        }

        public void SetDefaultPort(string port)
        {

        }
    }
}
