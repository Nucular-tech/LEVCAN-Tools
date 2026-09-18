using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace LEVCAN
{
    /// <summary>
    /// Thread-safe in-memory CAN bus that routes frames between all connected
    /// LC_Node instances without requiring real hardware.
    ///
    /// The LEVCAN native library has a single global send callback, so only one
    /// VirtualCanBus can be active at a time (same restriction as physical adapters).
    /// Frames are delivered to matching registered subscribers; echo suppression
    /// and hardware-like target address filtering are applied before delivery.
    /// </summary>
    public class VirtualCanBus : Icanbus
    {
        /// <summary>
        /// Identifies the node performing a transmission on the current thread,
        /// used for reliable echo suppression.
        /// </summary>
        [ThreadStatic]
        public static LC_Node? ActiveSenderNode;

        // ── Direct P/Invoke to install a filter callback below LC_Interface ───
        private delegate LC_Return NativeFilterDelegate(
            IntPtr reg, IntPtr mask, byte cnt);

        [DllImport("LEVCANlib", EntryPoint = "LC_Set_FilterCallback",
            CallingConvention = CallingConvention.StdCall)]
        private static extern void lib_SetFilterCallbackDirect(NativeFilterDelegate callback);

        private readonly NativeFilterDelegate _nativeSafeFilter;

        // ── Subscriber list ───────────────────────────────────────────────────

        private sealed class Subscriber
        {
            public readonly LC_Node Node;
            public readonly Action<uint, uint[], byte> Dispatch;
            public Subscriber(LC_Node node, Action<uint, uint[], byte> dispatch)
            {
                Node = node;
                Dispatch = dispatch;
            }
        }

        private readonly object _lock = new object();
        private readonly List<Subscriber> _subscribers = new List<Subscriber>();

        private int _txCounter, _rxCounter, _errors;

        // ── Constructor ───────────────────────────────────────────────────────

        public VirtualCanBus()
        {
            _nativeSafeFilter = SafeFilterCallback;
        }

        // ── Icanbus interface ─────────────────────────────────────────────────

        public int TXcounter { get => _txCounter; set => _txCounter = value; }
        public int RXcounter { get => _rxCounter; set => _rxCounter = value; }
        public int Errors    { get => _errors;    set => _errors = value; }
        public TimeSpan MaxRequestDelay { get; set; } = TimeSpan.Zero;
        public string Status => "Virtual CAN";

        public event EventHandler? OnDisconnected;
        public event EventHandler? OnConnected;
        public event Action? FrameActivity;

        // ── Node registration ─────────────────────────────────────────────────

        /// <summary>
        /// Register a node so its <paramref name="dispatch"/> callback receives
        /// frames addressed to it on the bus.
        /// </summary>
        public void Connect(LC_Node node, Action<uint, uint[], byte> dispatch)
        {
            lock (_lock)
            {
                foreach (var s in _subscribers)
                    if (s.Node == node) return;
                _subscribers.Add(new Subscriber(node, dispatch));
            }
        }

        /// <summary>Unregister a node from the bus.</summary>
        public void Disconnect(LC_Node node)
        {
            lock (_lock)
            {
                for (int i = _subscribers.Count - 1; i >= 0; i--)
                {
                    if (_subscribers[i].Node == node)
                    {
                        _subscribers.RemoveAt(i);
                        return;
                    }
                }
            }
        }

        // ── Global send callback (LEVCANlib → VirtualCanBus → destination nodes)

        private LC_Return SendCallback(uint header, uint[] data, byte length)
        {
            LC_HeaderPacked hp = new LC_HeaderPacked(header);

            // Snapshot under lock; dispatch without holding the lock.
            Subscriber[] snapshot;
            lock (_lock) { snapshot = _subscribers.ToArray(); }

            uint[] dataCopy = data != null ? (uint[])data.Clone() : new uint[2];
            byte   len = length;
            uint   hdr = header;

            foreach (var sub in snapshot)
            {
                // 1) Never deliver back to the transmitting node instance
                if (ActiveSenderNode != null && sub.Node == ActiveSenderNode)
                    continue;

                ushort subId = sub.Node.ShortName.NodeID;
                if (subId < (ushort)LC_Address.Broadcast)
                {
                    // 2) Never echo back to the node whose ID matches the source
                    if (hp.Source == subId)
                        continue;

                    // 3) CAN hardware filtering: drop unicast messages not addressed to this node
                    if (hp.Target != (byte)LC_Address.Broadcast && hp.Target != subId)
                        continue;
                }

                try
                {
                    // Synchronous dispatch directly into LC_ReceiveHandler pushes into
                    // the receiver node's native queue in strict CAN bus chronological order.
                    // If the receiver synchronously emits a reply on this thread, its own node
                    // is the sender for the duration of the re-entrant call.
                    var prevSender = ActiveSenderNode;
                    ActiveSenderNode = sub.Node;
                    try
                    {
                        sub.Dispatch(hdr, dataCopy, len);
                    }
                    finally
                    {
                        ActiveSenderNode = prevSender;
                    }
                    Interlocked.Increment(ref _rxCounter);
                    FrameActivity?.Invoke();
                }
                catch
                {
                    Interlocked.Increment(ref _errors);
                }
            }

            Interlocked.Increment(ref _txCounter);
            return LC_Return.Ok;
        }

        // ── Filter callbacks ──────────────────────────────────────────────────

        private LC_Return ManagedFilterCallback(uint reg, uint mask, byte index)
            => LC_Return.Ok;

        private LC_Return SafeFilterCallback(IntPtr reg, IntPtr mask, byte cnt)
            => LC_Return.Ok;

        // ── Icanbus lifecycle ─────────────────────────────────────────────────

        public void Open()
        {
            LC_Interface.SetFilterCallback(ManagedFilterCallback);
            LC_Interface.SetSendCallback(SendCallback);
            LC_Interface.InitQHandlers();

            lib_SetFilterCallbackDirect(_nativeSafeFilter);

            OnConnected?.Invoke(this, EventArgs.Empty);
        }

        public void Close()
        {
            LC_Interface.SetFilterCallback(null);
            LC_Interface.SetSendCallback(null);
            OnDisconnected?.Invoke(this, EventArgs.Empty);
        }

        public void SetDefaultPort(string port) { }
        public void SetBaudrate(int baudrate)   { }
    }
}
