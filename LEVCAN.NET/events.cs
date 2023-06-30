using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Drawing;

namespace LEVCAN.NET
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    struct lc_event_t
    {
        public IntPtr Text;
        public IntPtr Caption;
        public byte Buttons;
        public byte Icon;
        public byte Sender;
    }

    public struct LC_Event_t
    {
        public string Text;
        public string Caption;
        public ushort Sender;
        public LC_EventButtons_t Buttons;
        public LC_EventIcon_t Icon;
    }

    public enum LC_EventButtons_t
    {
        None, Ok, OkCancel, AbortRetryIgnore, YesNo, YesNoCancel, RetryCancel,
    }

    public enum LC_EventResult_t
    {
        //     Nothing is returned from the dialog box.
        None = 0,
        //     The dialog box return value is OK (usually sent from a button labeled OK).
        OK = 1,
        //     The dialog box return value is Cancel (usually sent from a button labeled Cancel).
        Cancel = 2,
        //     The dialog box return value is Abort (usually sent from a button labeled Abort).
        Abort = 3,
        //     The dialog box return value is Retry (usually sent from a button labeled Retry).
        Retry = 4,
        //     The dialog box return value is Ignore (usually sent from a button labeled Ignore).
        Ignore = 5,
        //     The dialog box return value is Yes (usually sent from a button labeled Yes).
        Yes = 6,
        //     The dialog box return value is No (usually sent from a button labeled No).
        No = 7
    }

    public enum LC_EventIcon_t
    {
        //     The message box contain no symbols.
        None = 0,
        //     The message box contains a symbol consisting of white X in a circle with a red
        //     background.
        Error = 16,
        //     The message box contains a symbol consisting of a question mark in a circle.
        Question = 32,
        //     The message box contains a symbol consisting of an exclamation point in a triangle
        //     with a yellow background.
        Warning = 48,
        //     The message box contains a symbol consisting of a lowercase letter i in a circle.
        Information = 64
    }

    public class LC_Events : LC_IObject
    {
        [DllImport("LEVCANlib", EntryPoint = "LC_EventReceive", CharSet = CharSet.Ansi)]
        private static extern LC_Return lc_EventReceive(IntPtr data, int dsize, byte sender, ref lc_event_t ev_out);

        public ushort Index { get; }
        public int Size { get; }
        public IntPtr Pointer { get; }
        public ushort Attributes { get { return (ushort)(lc_objectAttributes_internal.Function | lc_objectAttributes_internal.Writable); } }

        LC_Node _node;
        private Delegate _callback_api;
        private delegate void lc_callback(ref LC_NodeDescriptor descriptor, LC_Header header, IntPtr data, int size);
        private LC_EventCallback _callback_user;
        public delegate void LC_EventCallback(LC_Event_t eventData);

        public LC_Events(LC_Node node, LC_EventCallback eventCallback)
        {
            _node = node;
            _callback_user = eventCallback;
            Index = (ushort)LC_SystemMessage.Events;
            Size = -512; //up to 512 bytes of text

            _callback_api = new lc_callback(lc_EventCallback);
            Pointer = Marshal.GetFunctionPointerForDelegate(_callback_api);
        }

        private void lc_EventCallback(ref LC_NodeDescriptor descriptor, LC_Header header, IntPtr data, int size)
        {
            if (header.MsgID != (ushort)LC_SystemMessage.Events)
                return;
            lc_event_t raw_evnt = new lc_event_t();
            LC_Return status = lc_EventReceive(data, size, header.Source, ref raw_evnt);

            if (status == LC_Return.Ok)
            {
                var encoding = _node.GetNodeEncoding(header.Source);
                LC_Event_t event_value = new LC_Event_t();
                event_value.Sender = header.Source;
                event_value.Buttons = (LC_EventButtons_t)raw_evnt.Buttons;
                event_value.Caption = Text8z.PtrToString(raw_evnt.Caption, encoding, 128);
                Marshal.FreeHGlobal(raw_evnt.Caption);
                event_value.Text = Text8z.PtrToString(raw_evnt.Text, encoding, 512);
                Marshal.FreeHGlobal(raw_evnt.Text);
                event_value.Icon = (LC_EventIcon_t)raw_evnt.Icon;

                _callback_user?.Invoke(event_value);
            }
        }
    }
}
