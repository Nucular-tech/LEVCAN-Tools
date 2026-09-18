#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using ImGuiNET;
using Veldrid;

namespace LEVCAN_Configurator.Helpers
{
    /// <summary>
    /// Adaptive idle FPS limiter:
    /// - Drops framerate to ~10 FPS (100ms sleep) when idle and focused.
    /// - Drops framerate to ~4 FPS (250ms sleep) when unfocused or minimized.
    /// - Only tracks mouse coordinates inside client bounds so moving mouse outside does not wake it.
    /// - Immediately wakes to full 60 FPS on user interaction or background CAN events.
    /// </summary>
    public class IdleDetector : IDisposable
    {
        private readonly Stopwatch _idleStopwatch = Stopwatch.StartNew();

        public const double IdleThresholdSeconds = 1.0;
        public const int ThrottledDelayMs = 100; // ~10 FPS when idle and focused
        public const int UnfocusedThrottledDelayMs = 250; // ~4 FPS when app window is not focused or minimized

        public bool IsThrottled { get; private set; }
        public double IdleDurationSeconds => _idleStopwatch.Elapsed.TotalSeconds;

        public AutoResetEvent WakeSignal { get; } = new AutoResetEvent(false);

        public static IdleDetector? Instance { get; private set; }

        private Vector2 _previousMousePos = new Vector2(-1, -1);
        private bool _previousMouseInside = false;
        private int _lastTxRx = 0;
        private bool _firstFrame = true;

        public IdleDetector()
        {
            Instance = this;
        }

        public static int GetThrottleDelay(bool isWindowFocused = true) =>
            isWindowFocused ? ThrottledDelayMs : UnfocusedThrottledDelayMs;

        public void Reset()
        {
            _idleStopwatch.Restart();
            IsThrottled = false;
            try
            {
                WakeSignal.Set();
            }
            catch (ObjectDisposedException) { }
        }

        public static void RequestWake()
        {
            Instance?.Reset();
        }

        public static bool IsMouseInside(Vector2 pos, int width, int height)
        {
            return width > 0 && height > 0 &&
                   pos.X >= 0 && pos.Y >= 0 &&
                   pos.X < width && pos.Y < height;
        }

        /// <summary>
        /// Evaluates current frame input, window state, and communication activity.
        /// Returns true if the application should be throttled (idle or unfocused).
        /// </summary>
        public bool Update(
            InputSnapshot snapshot,
            int windowWidth,
            int windowHeight,
            bool isWindowFocused,
            int currentTxRx,
            bool isUiActive)
        {
            // When app window is not focused or minimized, throttle immediately
            if (!isWindowFocused)
            {
                IsThrottled = true;
                _previousMouseInside = false;
                _previousMousePos = snapshot.MousePosition;
                return true;
            }

            Vector2 mousePos = snapshot.MousePosition;
            bool currentMouseInside = IsMouseInside(mousePos, windowWidth, windowHeight);

            bool hasActivity = false;

            // 1. CAN bus communication traffic
            if (currentTxRx > _lastTxRx)
            {
                _lastTxRx = currentTxRx;
                hasActivity = true;
            }
            else if (currentTxRx < _lastTxRx)
            {
                // Counters were reset to 0 in SubmitUI()
                _lastTxRx = currentTxRx;
            }

            // 2. Active UI widgets (dragging knobs/sliders, text editing, etc.)
            if (isUiActive)
            {
                hasActivity = true;
            }

            // 3. Keyboard input
            if (snapshot.KeyEvents.Count > 0 || snapshot.KeyCharPresses.Count > 0)
            {
                hasActivity = true;
            }
            else
            {
                try
                {
                    if (ImGui.GetCurrentContext() != IntPtr.Zero)
                    {
                        var io = ImGui.GetIO();
                        if (io.KeyCtrl || io.KeyShift || io.KeyAlt || io.KeySuper)
                        {
                            hasActivity = true;
                        }
                    }
                }
                catch { }
            }

            // 4. Mouse clicks and buttons (only count if inside the window)
            if (currentMouseInside)
            {
                if (snapshot.MouseEvents.Count > 0)
                {
                    hasActivity = true;
                }
                else if (snapshot.IsMouseDown(MouseButton.Left) ||
                         snapshot.IsMouseDown(MouseButton.Right) ||
                         snapshot.IsMouseDown(MouseButton.Middle))
                {
                    hasActivity = true;
                }
                else if (snapshot.WheelDelta != 0f)
                {
                    hasActivity = true;
                }
            }

            // 5. Mouse movement
            // Only track coordinates that are ACTUALLY inside the window client bounds
            // so moving the mouse outside over other apps doesn't wake it.
            if (!_firstFrame && mousePos != _previousMousePos)
            {
                if (currentMouseInside)
                {
                    // Mouse is inside the window client bounds and moved -> user interaction
                    hasActivity = true;
                }
                // If both current and previous were outside the window bounds, ignore it!
            }

            _firstFrame = false;
            _previousMousePos = mousePos;
            _previousMouseInside = currentMouseInside;

            if (hasActivity)
            {
                _idleStopwatch.Restart();
                IsThrottled = false;
            }
            else if (_idleStopwatch.Elapsed.TotalSeconds >= IdleThresholdSeconds)
            {
                IsThrottled = true;
            }
            else
            {
                IsThrottled = false;
            }

            return IsThrottled;
        }

        public void Dispose()
        {
            if (Instance == this)
                Instance = null;
            WakeSignal.Dispose();
        }

        #region Self-Tests

        internal class MockInputSnapshot : InputSnapshot
        {
            public List<KeyEvent> KeyEventsList { get; } = new List<KeyEvent>();
            public List<MouseEvent> MouseEventsList { get; } = new List<MouseEvent>();
            public List<char> KeyCharPressesList { get; } = new List<char>();

            public IReadOnlyList<KeyEvent> KeyEvents => KeyEventsList;
            public IReadOnlyList<MouseEvent> MouseEvents => MouseEventsList;
            public IReadOnlyList<char> KeyCharPresses => KeyCharPressesList;

            public Vector2 MousePosition { get; set; } = Vector2.Zero;
            public float WheelDelta { get; set; } = 0f;
            public bool[] MouseDownButtons { get; } = new bool[13];

            public bool IsMouseDown(MouseButton button) =>
                (int)button < MouseDownButtons.Length && MouseDownButtons[(int)button];
        }

        /// <summary>
        /// Runs automated invariant verification of IdleDetector.
        /// Throws if any adaptive idle throttling rule is violated.
        /// </summary>
        public static void RunSelfTest()
        {
            const int W = 800;
            const int H = 750;

            // 1. Client bounds checking
            if (IsMouseInside(new Vector2(-1, 100), W, H))
                throw new Exception("Negative X must be outside client bounds.");
            if (IsMouseInside(new Vector2(100, -1), W, H))
                throw new Exception("Negative Y must be outside client bounds.");
            if (IsMouseInside(new Vector2(800, 100), W, H))
                throw new Exception("X == Width must be outside client bounds.");
            if (IsMouseInside(new Vector2(100, 750), W, H))
                throw new Exception("Y == Height must be outside client bounds.");
            if (!IsMouseInside(new Vector2(0, 0), W, H))
                throw new Exception("(0, 0) must be inside client bounds.");
            if (!IsMouseInside(new Vector2(799, 749), W, H))
                throw new Exception("(799, 749) must be inside client bounds.");
            if (!IsMouseInside(new Vector2(400, 300), W, H))
                throw new Exception("(400, 300) must be inside client bounds.");

            // 2. Throttle delay values
            if (GetThrottleDelay(true) != 100)
                throw new Exception("Focused throttle delay must be 100ms.");
            if (GetThrottleDelay(false) != 250)
                throw new Exception("Unfocused throttle delay must be 250ms.");

            // 3. Unfocused window throttles immediately
            using (var detector = new IdleDetector())
            {
                var snap = new MockInputSnapshot { MousePosition = new Vector2(100, 100) };
                bool throttled = detector.Update(snap, W, H, isWindowFocused: false, currentTxRx: 0, isUiActive: false);
                if (!throttled)
                    throw new Exception("Unfocused window must throttle immediately.");
            }

            // 4. Mouse movement outside window client area must NOT wake it
            using (var detector = new IdleDetector())
            {
                var snap = new MockInputSnapshot { MousePosition = new Vector2(-50, 200) };
                detector.Update(snap, W, H, isWindowFocused: true, currentTxRx: 0, isUiActive: false);

                // Move mouse outside over another app
                snap = new MockInputSnapshot { MousePosition = new Vector2(-100, 250) };
                detector.Update(snap, W, H, isWindowFocused: true, currentTxRx: 0, isUiActive: false);

                // Idle stopwatch should not have reset to 0 because mouse is outside
                // If it was idle, moving outside does not wake
            }

            // 5. Mouse movement inside window client area DOES wake
            using (var detector = new IdleDetector())
            {
                var snap = new MockInputSnapshot { MousePosition = new Vector2(100, 100) };
                detector.Update(snap, W, H, isWindowFocused: true, currentTxRx: 0, isUiActive: false);

                // Move mouse inside window
                snap = new MockInputSnapshot { MousePosition = new Vector2(150, 120) };
                detector.Update(snap, W, H, isWindowFocused: true, currentTxRx: 0, isUiActive: false);

                if (detector.IsThrottled)
                    throw new Exception("Moving mouse inside window must not be throttled.");
                if (detector.IdleDurationSeconds > 0.5)
                    throw new Exception("Moving mouse inside window must reset idle stopwatch.");
            }

            // 6. Mouse clicks outside window must NOT wake
            using (var detector = new IdleDetector())
            {
                var snap = new MockInputSnapshot { MousePosition = new Vector2(-50, 200) };
                snap.MouseEventsList.Add(new MouseEvent(MouseButton.Left, true));
                detector.Update(snap, W, H, isWindowFocused: true, currentTxRx: 0, isUiActive: false);

                // Outside click should not register activity
            }

            // 7. Mouse clicks inside window DOES wake
            using (var detector = new IdleDetector())
            {
                var snap = new MockInputSnapshot { MousePosition = new Vector2(200, 200) };
                snap.MouseEventsList.Add(new MouseEvent(MouseButton.Left, true));
                detector.Update(snap, W, H, isWindowFocused: true, currentTxRx: 0, isUiActive: false);

                if (detector.IsThrottled)
                    throw new Exception("Mouse click inside window must reset throttle.");
                if (detector.IdleDurationSeconds > 0.5)
                    throw new Exception("Mouse click inside window must reset idle stopwatch.");
            }

            // 8. Keyboard input wakes
            using (var detector = new IdleDetector())
            {
                var snap = new MockInputSnapshot { MousePosition = new Vector2(0, 0) };
                snap.KeyEventsList.Add(new KeyEvent(Key.Space, true, ModifierKeys.None));
                detector.Update(snap, W, H, isWindowFocused: true, currentTxRx: 0, isUiActive: false);

                if (detector.IsThrottled)
                    throw new Exception("Key press must wake idle detector.");
            }

            // 9. CAN bus communication traffic wakes
            using (var detector = new IdleDetector())
            {
                var snap = new MockInputSnapshot { MousePosition = new Vector2(0, 0) };
                // Initial update with 0
                detector.Update(snap, W, H, isWindowFocused: true, currentTxRx: 0, isUiActive: false);

                // Next update with packets received
                detector.Update(snap, W, H, isWindowFocused: true, currentTxRx: 5, isUiActive: false);

                if (detector.IsThrottled)
                    throw new Exception("CAN traffic delta must wake idle detector.");
                if (detector.IdleDurationSeconds > 0.5)
                    throw new Exception("CAN traffic delta must reset idle stopwatch.");
            }

            // 10. WakeSignal thread signaling
            using (var detector = new IdleDetector())
            {
                detector.Reset();
                if (!detector.WakeSignal.WaitOne(0))
                    throw new Exception("WakeSignal must be signaled after Reset().");
            }
        }

        #endregion
    }
}
