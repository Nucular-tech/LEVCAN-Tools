using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LEVCAN;

namespace LEVCAN.Tests
{
    [TestClass]
    public class NativeQueueTests
    {
        [DllImport("LEVCANlib", EntryPoint = "LC_QueueCreate", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr LC_QueueCreate(uint length, uint itemSize);

        [DllImport("LEVCANlib", EntryPoint = "LC_QueueDelete", CallingConvention = CallingConvention.StdCall)]
        private static extern void LC_QueueDelete(IntPtr queue);

        [DllImport("LEVCANlib", EntryPoint = "LC_QueueReset", CallingConvention = CallingConvention.StdCall)]
        private static extern void LC_QueueReset(IntPtr queue);

        [DllImport("LEVCANlib", EntryPoint = "LC_QueueSendToBack", CallingConvention = CallingConvention.StdCall)]
        private static extern int LC_QueueSendToBack(IntPtr queue, ref uint buffer, int ttwait);

        [DllImport("LEVCANlib", EntryPoint = "LC_QueueSendToBack", CallingConvention = CallingConvention.StdCall)]
        private static extern int LC_QueueSendToBackPtr(IntPtr queue, IntPtr buffer, int ttwait);

        [DllImport("LEVCANlib", EntryPoint = "LC_QueueReceive", CallingConvention = CallingConvention.StdCall)]
        private static extern int LC_QueueReceive(IntPtr queue, ref uint buffer, int ttwait);

        [DllImport("LEVCANlib", EntryPoint = "LC_QueueReceive", CallingConvention = CallingConvention.StdCall)]
        private static extern int LC_QueueReceivePtr(IntPtr queue, IntPtr buffer, int ttwait);

        [DllImport("LEVCANlib", EntryPoint = "LC_QueueStored", CallingConvention = CallingConvention.StdCall)]
        private static extern uint LC_QueueStored(IntPtr queue);

        [TestMethod]
        public void QueueCreate_ValidParams_ReturnsNonNullPointer()
        {
            IntPtr q = LC_QueueCreate(5, sizeof(uint));
            Assert.AreNotEqual(IntPtr.Zero, q);
            try
            {
                Assert.AreEqual(0u, LC_QueueStored(q));
            }
            finally
            {
                LC_QueueDelete(q);
            }
        }

        [TestMethod]
        public void QueueCreate_ZeroParams_ReturnsNullPointer()
        {
            Assert.AreEqual(IntPtr.Zero, LC_QueueCreate(0, sizeof(uint)));
            Assert.AreEqual(IntPtr.Zero, LC_QueueCreate(5, 0));
        }

        [TestMethod]
        public void QueueSendAndReceive_SingleItem_Success()
        {
            IntPtr q = LC_QueueCreate(5, sizeof(uint));
            Assert.AreNotEqual(IntPtr.Zero, q);
            try
            {
                uint valToSend = 0xDEADBEEF;
                int sendRes = LC_QueueSendToBack(q, ref valToSend, 0);
                Assert.AreEqual(1, sendRes);
                Assert.AreEqual(1u, LC_QueueStored(q));

                uint valReceived = 0;
                int recRes = LC_QueueReceive(q, ref valReceived, 0);
                Assert.AreEqual(1, recRes);
                Assert.AreEqual(valToSend, valReceived);
                Assert.AreEqual(0u, LC_QueueStored(q));
            }
            finally
            {
                LC_QueueDelete(q);
            }
        }

        [TestMethod]
        public void QueueSendAndReceive_FifoOrdering()
        {
            IntPtr q = LC_QueueCreate(10, sizeof(uint));
            Assert.AreNotEqual(IntPtr.Zero, q);
            try
            {
                for (uint i = 1; i <= 5; i++)
                {
                    int res = LC_QueueSendToBack(q, ref i, 0);
                    Assert.AreEqual(1, res);
                }
                Assert.AreEqual(5u, LC_QueueStored(q));

                for (uint i = 1; i <= 5; i++)
                {
                    uint val = 0;
                    int res = LC_QueueReceive(q, ref val, 0);
                    Assert.AreEqual(1, res);
                    Assert.AreEqual(i, val);
                }
                Assert.AreEqual(0u, LC_QueueStored(q));
            }
            finally
            {
                LC_QueueDelete(q);
            }
        }

        [TestMethod]
        public void QueueFull_RejectsAdditionalItemWhenNonBlocking()
        {
            IntPtr q = LC_QueueCreate(2, sizeof(uint));
            Assert.AreNotEqual(IntPtr.Zero, q);
            try
            {
                uint a = 1, b = 2, c = 3;
                Assert.AreEqual(1, LC_QueueSendToBack(q, ref a, 0));
                Assert.AreEqual(1, LC_QueueSendToBack(q, ref b, 0));
                Assert.AreEqual(2u, LC_QueueStored(q));

                // Third item must fail because capacity is 2
                Assert.AreEqual(0, LC_QueueSendToBack(q, ref c, 0));
                Assert.AreEqual(2u, LC_QueueStored(q));
            }
            finally
            {
                LC_QueueDelete(q);
            }
        }

        [TestMethod]
        public void QueueReceive_EmptyQueue_ReturnsZeroOnTimeout()
        {
            IntPtr q = LC_QueueCreate(5, sizeof(uint));
            Assert.AreNotEqual(IntPtr.Zero, q);
            try
            {
                uint val = 0;
                var sw = Stopwatch.StartNew();
                int res = LC_QueueReceive(q, ref val, 50);
                sw.Stop();

                Assert.AreEqual(0, res);
                Assert.IsTrue(sw.ElapsedMilliseconds >= 30, $"Expected at least 30ms timeout wait, took {sw.ElapsedMilliseconds}ms");
            }
            finally
            {
                LC_QueueDelete(q);
            }
        }

        [TestMethod]
        public void QueueReset_ClearsAllItems()
        {
            IntPtr q = LC_QueueCreate(5, sizeof(uint));
            Assert.AreNotEqual(IntPtr.Zero, q);
            try
            {
                uint a = 10, b = 20;
                LC_QueueSendToBack(q, ref a, 0);
                LC_QueueSendToBack(q, ref b, 0);
                Assert.AreEqual(2u, LC_QueueStored(q));

                LC_QueueReset(q);
                Assert.AreEqual(0u, LC_QueueStored(q));

                uint val = 0;
                Assert.AreEqual(0, LC_QueueReceive(q, ref val, 0));
            }
            finally
            {
                LC_QueueDelete(q);
            }
        }

        [TestMethod]
        public void QueueConcurrent_ProducerConsumer_NoDataLoss()
        {
            IntPtr q = LC_QueueCreate(16, sizeof(uint));
            Assert.AreNotEqual(IntPtr.Zero, q);
            const int count = 200;
            var received = new List<uint>();

            try
            {
                var consumer = Task.Run(() =>
                {
                    for (int i = 0; i < count; i++)
                    {
                        uint val = 0;
                        int res = LC_QueueReceive(q, ref val, 1000);
                        Assert.AreEqual(1, res, $"Failed to receive item {i}");
                        received.Add(val);
                    }
                });

                var producer = Task.Run(() =>
                {
                    for (uint i = 0; i < count; i++)
                    {
                        uint val = i;
                        int res = LC_QueueSendToBack(q, ref val, 1000);
                        Assert.AreEqual(1, res, $"Failed to send item {i}");
                        Thread.Sleep(1);
                    }
                });

                Task.WaitAll(producer, consumer);
                Assert.AreEqual(count, received.Count);
                for (uint i = 0; i < count; i++)
                {
                    Assert.AreEqual(i, received[(int)i]);
                }
            }
            finally
            {
                LC_QueueDelete(q);
            }
        }

        [TestMethod]
        public void LC_Interface_IsReady_WorksWithoutManagedQueues()
        {
            // Verify IsReady() becomes true once callbacks are set, without requiring any managed queue init
            LC_Interface.SetSendCallback((header, data, length) => LC_Return.Ok);
            LC_Interface.SetFilterCallback((reg, mask, index) => LC_Return.Ok);

            Assert.IsTrue(LC_Interface.IsReady());
        }

        [TestMethod]
        public void LC_Node_Creation_SucceedsWithNativeQueues()
        {
            LC_Interface.SetSendCallback((header, data, length) => LC_Return.Ok);
            LC_Interface.SetFilterCallback((reg, mask, index) => LC_Return.Ok);
            Assert.IsTrue(LC_Interface.IsReady());

            var node = new LC_Node(42);
            Assert.AreNotEqual(IntPtr.Zero, node.DescriptorPtr);
            Assert.AreEqual((ushort)42, node.ShortName.NodeID);
        }
    }
}
