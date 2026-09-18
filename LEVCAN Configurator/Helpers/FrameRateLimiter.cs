#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace LEVCAN_Configurator.Helpers
{
    /// <summary>
    /// High-resolution frame rate limiter and pacer:
    /// - Caps active rendering to a strict target FPS (e.g. 60 FPS / ~16.67ms per frame).
    /// - Uses Windows timeBeginPeriod(1) during its lifetime for 1ms timer interrupt resolution.
    /// - Performs coarse sleep for bulk remaining time and fine spin-wait for sub-2ms precision.
    /// - Prevents 120Hz/144Hz monitors from over-rendering.
    /// </summary>
    public sealed class FrameRateLimiter : IDisposable
    {
        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        private static extern uint TimeBeginPeriod(uint uMilliseconds);

        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        private static extern uint TimeEndPeriod(uint uMilliseconds);

        public double TargetFps { get; }
        public long TargetFrameTicks { get; }
        public double TargetFrameTimeMs { get; }

        private readonly Stopwatch _stopwatch;
        private readonly bool _periodStarted;
        private bool _disposed;

        public FrameRateLimiter(double targetFps = 60.0, Stopwatch? stopwatch = null)
        {
            if (targetFps <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(targetFps), "Target FPS must be positive.");

            TargetFps = targetFps;
            TargetFrameTicks = (long)Math.Round((double)Stopwatch.Frequency / targetFps);
            TargetFrameTimeMs = 1000.0 / targetFps;
            _stopwatch = stopwatch ?? Stopwatch.StartNew();

            if (OperatingSystem.IsWindows())
            {
                try
                {
                    TimeBeginPeriod(1);
                    _periodStarted = true;
                }
                catch { }
            }
        }

        public long GetTicks() => _stopwatch.ElapsedTicks;

        /// <summary>
        /// Computes the delta seconds between the current frame start and previous frame start ticks.
        /// Strictly bounds the delta so that it never represents a frame rate higher than TargetFps
        /// (i.e. deltaSeconds is never less than 1.0 / TargetFps), nor larger than maxDeltaSeconds.
        /// </summary>
        public float ComputeDeltaSeconds(long currentTicks, long previousTicks, float maxDeltaSeconds = 0.25f)
        {
            float delta = (float)(currentTicks - previousTicks) / Stopwatch.Frequency;
            float minDelta = 1f / (float)TargetFps;
            return Math.Clamp(delta, minDelta, maxDeltaSeconds);
        }

        /// <summary>
        /// Returns the initial 'lastTicks' value for application startup,
        /// ensuring the very first frame delta is exactly 1 / TargetFps (e.g. ~16.67ms for 60 FPS)
        /// with no startup spike.
        /// </summary>
        public long GetInitialLastTicks() => GetTicks() - TargetFrameTicks;

        /// <summary>
        /// Waits until the frame duration since frameStartTicks reaches TargetFrameTicks.
        /// If the elapsed time already exceeds TargetFrameTicks (e.g. throttled or heavy workload), returns immediately.
        /// </summary>
        public void WaitNextFrame(long frameStartTicks)
        {
            while (true)
            {
                long elapsedTicks = _stopwatch.ElapsedTicks - frameStartTicks;
                if (elapsedTicks < 0)
                    elapsedTicks = 0;

                long remainingTicks = TargetFrameTicks - elapsedTicks;
                if (remainingTicks <= 0)
                    break;

                double remainingMs = (double)remainingTicks * 1000.0 / Stopwatch.Frequency;
                if (remainingMs > 2.5)
                {
                    int sleepMs = (int)(remainingMs - 2.0);
                    if (sleepMs > 0)
                        Thread.Sleep(sleepMs);
                    else
                        Thread.Sleep(1);
                }
                else
                {
                    Thread.SpinWait(10);
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            if (_periodStarted && OperatingSystem.IsWindows())
            {
                try
                {
                    TimeEndPeriod(1);
                }
                catch { }
            }
        }

        #region Self-Tests

        /// <summary>
        /// Runs automated invariant verification of FrameRateLimiter.
        /// Throws if any frame limiter rule or invariant is violated.
        /// </summary>
        public static void RunSelfTest()
        {
            // 1. Parameter validation
            try
            {
                using var invalid = new FrameRateLimiter(0);
                throw new Exception("FrameRateLimiter must reject 0 FPS.");
            }
            catch (ArgumentOutOfRangeException) { }

            try
            {
                using var invalid = new FrameRateLimiter(-10);
                throw new Exception("FrameRateLimiter must reject negative FPS.");
            }
            catch (ArgumentOutOfRangeException) { }

            // 2. Calculation invariants for 60 FPS
            using (var limiter60 = new FrameRateLimiter(60.0))
            {
                if (Math.Abs(limiter60.TargetFrameTimeMs - 16.666666666666668) > 0.001)
                    throw new Exception("TargetFrameTimeMs for 60 FPS must be ~16.667ms.");

                long expectedTicks = (long)Math.Round((double)Stopwatch.Frequency / 60.0);
                if (limiter60.TargetFrameTicks != expectedTicks)
                    throw new Exception("TargetFrameTicks does not match expected ticks.");

                // 3. If elapsedTicks >= TargetFrameTicks, WaitNextFrame must return immediately without blocking
                var sw = Stopwatch.StartNew();
                long pastStart = limiter60.GetTicks() - limiter60.TargetFrameTicks - 1000;
                limiter60.WaitNextFrame(pastStart);
                sw.Stop();
                if (sw.ElapsedMilliseconds > 5)
                    throw new Exception("WaitNextFrame must return immediately when elapsed >= target.");

                // 4. Verification of frame pacing over 5 frames
                var pacingSw = Stopwatch.StartNew();
                for (int i = 0; i < 5; i++)
                {
                    long start = limiter60.GetTicks();
                    Thread.Sleep(1); // simulate minimal workload
                    limiter60.WaitNextFrame(start);
                }
                pacingSw.Stop();

                double totalMs = pacingSw.Elapsed.TotalMilliseconds;
                if (totalMs < 75.0 || totalMs > 150.0)
                    throw new Exception($"Pacing self-test out of bounds: 5 frames took {totalMs:F2}ms (expected ~83.3ms).");

                // 5. Delta clamping and startup initial ticks invariant
                long initLast = limiter60.GetInitialLastTicks();
                long ticksNow = limiter60.GetTicks();
                float initialDelta = limiter60.ComputeDeltaSeconds(ticksNow, initLast);
                float expectedMinDelta = 1f / 60f;
                if (Math.Abs(initialDelta - expectedMinDelta) > 0.005f)
                    throw new Exception($"Initial delta ({initialDelta}) must equal expected min delta ({expectedMinDelta}).");

                // Microsecond gap (simulating startup bug) must clamp to min delta (1/60s), never drop to 0.001s
                float microGapDelta = limiter60.ComputeDeltaSeconds(ticksNow, ticksNow - 10);
                if (Math.Abs(microGapDelta - expectedMinDelta) > 0.0001f)
                    throw new Exception($"Microsecond gap must clamp to min delta ({expectedMinDelta}), got {microGapDelta}.");

                // Simulated 120Hz monitor frame (8.33ms) must clamp to 1/60s (16.67ms)
                long ticks8ms = (long)(Stopwatch.Frequency * 0.00833);
                float fastDelta = limiter60.ComputeDeltaSeconds(ticksNow, ticksNow - ticks8ms);
                if (Math.Abs(fastDelta - expectedMinDelta) > 0.0001f)
                    throw new Exception($"Fast frame delta must clamp to min delta ({expectedMinDelta}), got {fastDelta}.");

                // Throttled frame (100ms) must NOT clamp to 1/60s, but remain ~100ms
                long ticks100ms = (long)(Stopwatch.Frequency * 0.100);
                float slowDelta = limiter60.ComputeDeltaSeconds(ticksNow, ticksNow - ticks100ms);
                if (Math.Abs(slowDelta - 0.100f) > 0.005f)
                    throw new Exception($"Slow frame delta must preserve ~100ms, got {slowDelta}.");
            }
        }

        #endregion
    }
}
