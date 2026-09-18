using System;
using System.Diagnostics;
using System.Threading;
using LEVCAN_Configurator.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LEVCAN.Tests
{
    [TestClass]
    public class FrameRateLimiterTests
    {
        [TestMethod]
        public void TargetTimeAndTicks_ComputedCorrectly()
        {
            using var limiter = new FrameRateLimiter(60.0);
            Assert.AreEqual(60.0, limiter.TargetFps);
            Assert.AreEqual(1000.0 / 60.0, limiter.TargetFrameTimeMs, 0.001);

            long expectedTicks = (long)Math.Round((double)Stopwatch.Frequency / 60.0);
            Assert.AreEqual(expectedTicks, limiter.TargetFrameTicks);
        }

        [TestMethod]
        public void InvalidFps_ThrowsArgumentOutOfRangeException()
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new FrameRateLimiter(0));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new FrameRateLimiter(-30));
        }

        [TestMethod]
        public void AlreadyElapsed_ReturnsImmediately()
        {
            using var limiter = new FrameRateLimiter(60.0);
            var sw = Stopwatch.StartNew();
            long pastTicks = limiter.GetTicks() - limiter.TargetFrameTicks - 5000;
            limiter.WaitNextFrame(pastTicks);
            sw.Stop();

            Assert.IsTrue(sw.ElapsedMilliseconds < 5, $"WaitNextFrame should not block when elapsed >= target, took {sw.ElapsedMilliseconds}ms");
        }

        [TestMethod]
        public void PacingFrames_CapsAtTargetRate()
        {
            using var limiter = new FrameRateLimiter(60.0);
            var sw = Stopwatch.StartNew();

            const int frames = 10;
            for (int i = 0; i < frames; i++)
            {
                long start = limiter.GetTicks();
                Thread.Sleep(2); // simulate work
                limiter.WaitNextFrame(start);
            }
            sw.Stop();

            // 10 frames at 60 FPS is ~166.7ms.
            // In test environment, verify elapsed time is within [150ms, 300ms].
            double totalMs = sw.Elapsed.TotalMilliseconds;
            Assert.IsTrue(totalMs >= 150.0, $"10 frames completed too fast ({totalMs:F2}ms < 150ms). FPS was not capped.");
            Assert.IsTrue(totalMs <= 300.0, $"10 frames took too long ({totalMs:F2}ms > 300ms).");
        }

        [TestMethod]
        public void ComputeDeltaSeconds_NeverExceedsTargetFps()
        {
            using var limiter = new FrameRateLimiter(60.0);
            long now = limiter.GetTicks();
            float minDelta = 1f / 60f;

            // Zero or tiny difference (simulating first frame startup bug or sub-millisecond gap)
            Assert.AreEqual(minDelta, limiter.ComputeDeltaSeconds(now, now), 0.0001f);
            Assert.AreEqual(minDelta, limiter.ComputeDeltaSeconds(now, now - 5), 0.0001f);

            // 120Hz frame (8.33ms) must be clamped to 60 FPS (~16.67ms)
            long ticks120Hz = (long)(Stopwatch.Frequency / 120.0);
            Assert.AreEqual(minDelta, limiter.ComputeDeltaSeconds(now, now - ticks120Hz), 0.0001f);

            // 144Hz frame (6.94ms) must be clamped to 60 FPS (~16.67ms)
            long ticks144Hz = (long)(Stopwatch.Frequency / 144.0);
            Assert.AreEqual(minDelta, limiter.ComputeDeltaSeconds(now, now - ticks144Hz), 0.0001f);

            // Normal 60Hz frame
            long ticks60Hz = (long)(Stopwatch.Frequency / 60.0);
            Assert.AreEqual(minDelta, limiter.ComputeDeltaSeconds(now, now - ticks60Hz), 0.0001f);

            // Slower frame (10 FPS / 100ms) - preserves slower rate for idle throttling
            long ticks10Hz = (long)(Stopwatch.Frequency / 10.0);
            Assert.AreEqual(0.1f, limiter.ComputeDeltaSeconds(now, now - ticks10Hz), 0.001f);
        }

        [TestMethod]
        public void InitialLastTicks_ProducesTargetFpsDeltaOnFirstFrame()
        {
            using var limiter = new FrameRateLimiter(60.0);
            long initialLast = limiter.GetInitialLastTicks();
            long firstFrameStart = limiter.GetTicks();
            float delta = limiter.ComputeDeltaSeconds(firstFrameStart, initialLast);

            // First frame delta must be exactly ~0.01667s (60 FPS), never ~0.001s (1000 FPS)
            Assert.AreEqual(1f / 60f, delta, 0.001f);
        }

        [TestMethod]
        public void PacingWithSimulatedCanTraffic_NeverExceedsTargetFps()
        {
            using var limiter = new FrameRateLimiter(60.0);
            using var wakeSignal = new AutoResetEvent(false);

            // Simulate high-frequency CAN packet arrival (background thread signaling at 1000Hz)
            using var cts = new CancellationTokenSource();
            var canThread = new Thread(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    wakeSignal.Set();
                    Thread.Sleep(1);
                }
            });
            canThread.Start();

            var sw = Stopwatch.StartNew();
            const int frames = 10;
            for (int i = 0; i < frames; i++)
            {
                long start = limiter.GetTicks();
                Thread.Sleep(2); // simulate render workload
                limiter.WaitNextFrame(start);
            }
            sw.Stop();
            cts.Cancel();
            canThread.Join();

            // 10 frames at 60 FPS is ~166.7ms. Pacing must strictly enforce >= 150ms cap despite 1000Hz CAN packets.
            double totalMs = sw.Elapsed.TotalMilliseconds;
            Assert.IsTrue(totalMs >= 150.0, $"10 frames completed too fast ({totalMs:F2}ms < 150ms) during CAN traffic flood.");
            Assert.IsTrue(totalMs <= 300.0, $"10 frames took too long ({totalMs:F2}ms > 300ms).");
        }

        [TestMethod]
        public void RunSelfTest_Passes()
        {
            FrameRateLimiter.RunSelfTest();
        }
    }
}
