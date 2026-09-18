using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LEVCAN;

namespace LEVCAN.Tests
{
    [TestClass]
    public class HeapAndSendDataTests
    {
        private static void WaitForOnline(LC_Node node, int timeoutMs = 3000)
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (node.State == LC_NodeState.Online)
                    return;
                Thread.Sleep(20);
            }
            Assert.Fail("Node did not go Online within timeout.");
        }

        [TestMethod]
        public void LCMallocAndFree_BasicAllocationAndFree_Succeeds()
        {
            IntPtr ptr = LC_Node.LC_Malloc((IntPtr)128);
            Assert.AreNotEqual(IntPtr.Zero, ptr);

            try
            {
                byte[] writeData = new byte[128];
                for (int i = 0; i < writeData.Length; i++)
                    writeData[i] = (byte)(i & 0xFF);

                Marshal.Copy(writeData, 0, ptr, writeData.Length);

                byte[] readData = new byte[128];
                Marshal.Copy(ptr, readData, 0, readData.Length);

                CollectionAssert.AreEqual(writeData, readData);
            }
            finally
            {
                LC_Node.LC_Free(ptr);
            }
        }

        [TestMethod]
        public void LCFree_NullPointer_DoesNotCrash()
        {
            // Should not throw or crash
            LC_Node.LC_Free(IntPtr.Zero);
        }

        [TestMethod]
        public void SendData_ShortPayload_FastSend_DoesNotCorruptHeap()
        {
            var bus = new VirtualCanBus();
            bus.Open();

            var clientNode = new LC_Node(20);
            bus.Connect(clientNode,
                (hdr, data, len) => LC_Interface.lib_ReceiveHandler(clientNode.DescriptorPtr, hdr, data, len));
            clientNode.StartNode();
            WaitForOnline(clientNode);

            // Short payload (<= 8 bytes) uses the immediate fast-send branch in levcan.c
            // which frees object->Address directly with lcfree.
            byte[] shortPayload = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            for (int i = 0; i < 20; i++)
            {
                var ret = clientNode.SendData(shortPayload, (byte)LC_Address.Broadcast, (ushort)LC_SystemMessage.DateTime);
                Assert.AreEqual(LC_Return.Ok, ret);
            }

            bus.Disconnect(clientNode);
            bus.Close();
        }

        [TestMethod]
        public void SendData_LongPayload_BufferedSend_DoesNotCorruptHeap()
        {
            var bus = new VirtualCanBus();
            bus.Open();

            var clientNode = new LC_Node(21);
            bus.Connect(clientNode,
                (hdr, data, len) => LC_Interface.lib_ReceiveHandler(clientNode.DescriptorPtr, hdr, data, len));
            clientNode.StartNode();
            WaitForOnline(clientNode);

            // Long payload (> 8 bytes) allocates newTXobj with TXcleanup flag in levcan.c
            // and cleans up via garbage collector or attempt completion with lcfree.
            byte[] longPayload = new byte[64];
            for (int i = 0; i < longPayload.Length; i++)
                longPayload[i] = (byte)i;

            for (int i = 0; i < 10; i++)
            {
                var ret = clientNode.SendData(longPayload, (byte)LC_Address.Broadcast, (ushort)LC_SystemMessage.DateTime);
                Assert.AreEqual(LC_Return.Ok, ret);
            }

            Thread.Sleep(200);

            bus.Disconnect(clientNode);
            bus.Close();
        }

        [TestMethod]
        public void SendData_ZeroLength_SucceedsWithoutCrash()
        {
            var bus = new VirtualCanBus();
            bus.Open();

            var clientNode = new LC_Node(22);
            bus.Connect(clientNode,
                (hdr, data, len) => LC_Interface.lib_ReceiveHandler(clientNode.DescriptorPtr, hdr, data, len));
            clientNode.StartNode();
            WaitForOnline(clientNode);

            var ret = clientNode.SendData(Array.Empty<byte>(), (byte)LC_Address.Broadcast, (ushort)LC_SystemMessage.DateTime);
            Assert.AreEqual(LC_Return.Ok, ret);

            bus.Disconnect(clientNode);
            bus.Close();
        }

        [TestMethod]
        public void LCNode_CreateAndDispose_Succeeds()
        {
            using var node = new LC_Node(42);
            Assert.AreNotEqual(IntPtr.Zero, node.DescriptorPtr);
            Assert.AreEqual((byte)42, node.ShortName.NodeID);
        }

        [TestMethod]
        public void LCNode_DirectPInvokeExports_ExistAndWork()
        {
            IntPtr desc = LC_Node.LC_Node_Create(55);
            Assert.AreNotEqual(IntPtr.Zero, desc);
            try
            {
                uint[] serial = new uint[] { 10, 20, 30, 40 };
                LC_Node.LC_Node_SetIdentity(desc, "TestNode", "TestDev", "TestVendor", 1251, serial);
                var sn = LC_Node.LC_Node_GetShortName(desc);
                Assert.AreEqual((byte)55, sn.NodeID);
            }
            finally
            {
                LC_Node.LC_Node_Destroy(desc);
            }
        }
    }
}
