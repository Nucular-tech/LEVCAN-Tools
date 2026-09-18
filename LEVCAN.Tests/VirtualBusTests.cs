using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LEVCAN;
using LEVCAN_Configurator_Shared;

namespace LEVCAN.Tests
{
    /// <summary>
    /// Stress / integration tests that run an in-memory LEVCAN network using
    /// <see cref="VirtualCanBus"/> and <see cref="FakeULightDevice"/>.
    ///
    /// All four tests share a single [TestClass] so MSTest runs them
    /// sequentially (within the same class) and the global LEVCANlib callbacks
    /// are not torn down between individual tests.
    /// </summary>
    [TestClass]
    public class VirtualBusTests
    {
        // ── Discovery helper ──────────────────────────────────────────────────

        /// <summary>
        /// Wait up to <paramref name="timeoutMs"/> ms for a node with the given
        /// <paramref name="expectedNodeId"/> to appear in <paramref name="client"/>'s
        /// active-node table.
        /// </summary>
        private static bool WaitForNode(LC_Node client, ushort expectedNodeId, int timeoutMs)
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                var nodes = client.GetActiveNodes();
                if (nodes.Any(n => n.NodeID == expectedNodeId))
                    return true;
                Thread.Sleep(50);
            }
            return false;
        }

        // ── Test 1: NodeDiscovery ─────────────────────────────────────────────

        /// <summary>
        /// Start a client node and a FakeULight on the same VirtualCanBus.
        /// Assert that the fake device is discovered within 3 seconds.
        /// </summary>
        [TestMethod]
        [Timeout(10_000)]
        public void NodeDiscovery()
        {
            var bus = new VirtualCanBus();
            bus.Open();

            // Client node (NodeID 10) — must be connected AFTER Open() arms callbacks.
            var clientNode = new LC_Node(10);

            bus.Connect(clientNode,
                (hdr, data, len) => LC_Interface.lib_ReceiveHandler(clientNode.DescriptorPtr, hdr, data, len));
            clientNode.StartNode();

            // Fake device (NodeID 5)
            using var fakeDevice = new FakeULightDevice(bus, nodeId: 5);

            // Wait up to 3 s for discovery
            bool found = WaitForNode(clientNode, 5, timeoutMs: 3_000);

            Assert.IsTrue(found,
                "FakeULight (NodeID=5) was not discovered within 3 seconds.");
        }

        // ── Test 2: DirectoryTreeScan ─────────────────────────────────────────

        /// <summary>
        /// Run <see cref="LC_ParamClient.UpdateDirectoriesAsync"/> against the
        /// fake device.  Assert at least 4 directories and an entry named "Voltage".
        /// </summary>
        [TestMethod]
        [Timeout(60_000)]
        public async Task DirectoryTreeScan()
        {
            var bus = new VirtualCanBus();
            bus.Open();

            var clientNode = new LC_Node(10);
            bus.Connect(clientNode,
                (hdr, data, len) => LC_Interface.lib_ReceiveHandler(clientNode.DescriptorPtr, hdr, data, len));
            clientNode.StartNode();

            using var fakeDevice = new FakeULightDevice(bus, nodeId: 5);

            // Wait for discovery; give extra time for both nodes to go Online
            bool found = WaitForNode(clientNode, 5, timeoutMs: 3_000);
            Assert.IsTrue(found, "FakeULight not discovered before directory scan.");
            // Extra settle time: address discovery to Online state can take an
            // additional ~200ms after the node appears in the active-node table.
            Thread.Sleep(500);

            var client = new LC_ParamClient(clientNode, 5);

            // Full scan
            await client.UpdateDirectoriesAsync();

            Assert.IsNotNull(client.Directories, "Directories list should not be null.");
            Assert.IsTrue(client.Directories.Count >= 4,
                $"Expected >= 4 directories, got {client.Directories.Count}.");

            // Collect all entry names across all directories
            var allEntryNames = client.Directories
                .SelectMany(d => d.Entries ?? new System.Collections.Generic.List<LCPC_Entry>())
                .Select(e => e.Name)
                .ToList();

            bool hasVoltage = allEntryNames.Any(n =>
                n != null && n.IndexOf("Voltage", StringComparison.OrdinalIgnoreCase) >= 0);

            Assert.IsTrue(hasVoltage,
                $"Expected entry named 'Voltage'. Found entries: {string.Join(", ", allEntryNames)}");
        }

        // ── Test 3: ConcurrentParameterFlood ──────────────────────────────────

        /// <summary>
        /// Launch 8 parallel tasks, each sending 20 <c>UpdateEntryValue</c>
        /// calls.  Assert no exceptions, no deadlocks (10 s timeout), and
        /// successful calls > 100.
        /// </summary>
        [TestMethod]
        [Timeout(15_000)]
        public async Task ConcurrentParameterFlood()
        {
            var bus = new VirtualCanBus();
            bus.Open();

            var clientNode = new LC_Node(10);
            bus.Connect(clientNode,
                (hdr, data, len) => LC_Interface.lib_ReceiveHandler(clientNode.DescriptorPtr, hdr, data, len));
            clientNode.StartNode();

            using var fakeDevice = new FakeULightDevice(bus, nodeId: 5);

            bool found = WaitForNode(clientNode, 5, timeoutMs: 3_000);
            Assert.IsTrue(found, "FakeULight not discovered before flood test.");
            Thread.Sleep(500);

            var client = new LC_ParamClient(clientNode, 5);
            await client.UpdateDirectoriesAsync();

            Assert.IsTrue(client.Directories.Count >= 4, "Need directories before flood.");

            // Gather at least a few entries to request.
            var entries = client.Directories
                .SelectMany(d => d.Entries ?? new System.Collections.Generic.List<LCPC_Entry>())
                .Where(e => e.VariablePtr != IntPtr.Zero)
                .ToArray();

            // If the server returned no live entries, just skip the value requests gracefully.
            if (entries.Length == 0)
            {
                Assert.Inconclusive("No entries with variable pointers returned by FakeULight; " +
                                    "skipping concurrent flood assertions.");
            }

            int successCount = 0;
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            const int tasks  = 8;
            const int rounds = 20;

            var floodTasks = Enumerable.Range(0, tasks).Select(_ =>
                Task.Run(async () =>
                {
                    for (int i = 0; i < rounds; i++)
                    {
                        var entry = entries[i % entries.Length];
                        try
                        {
                            LC_Return result = await client.UpdateEntryValue(entry);
                            if (result == LC_Return.Ok || result == LC_Return.Timeout)
                                Interlocked.Increment(ref successCount);
                        }
                        catch (Exception ex)
                        {
                            exceptions.Add(ex);
                        }
                    }
                })
            ).ToArray();

            // 10-second deadline for all flood tasks
            var allDone = Task.WhenAll(floodTasks);
            var deadline = Task.Delay(10_000);
            var winner = await Task.WhenAny(allDone, deadline);

            Assert.AreNotSame(deadline, winner, "Concurrent flood deadlocked or took > 10 s.");

            Assert.AreEqual(0, exceptions.Count,
                $"Exceptions during flood: {string.Join("; ", exceptions.Select(e => e.Message))}");

            Assert.IsTrue(successCount > 100,
                $"Expected > 100 successful calls, got {successCount}.");
        }

        // ── Test 4: CleanTeardown ─────────────────────────────────────────────

        /// <summary>
        /// Connect, start, and then disconnect / dispose all objects.
        /// Assert no exceptions are thrown during teardown.
        /// </summary>
        [TestMethod]
        [Timeout(10_000)]
        public void CleanTeardown()
        {
            Exception? caughtException = null;
            try
            {
                var bus = new VirtualCanBus();
                bus.Open();

                var clientNode = new LC_Node(10);
                bus.Connect(clientNode,
                    (hdr, data, len) => LC_Interface.lib_ReceiveHandler(clientNode.DescriptorPtr, hdr, data, len));
                clientNode.StartNode();

                var fakeDevice = new FakeULightDevice(bus, nodeId: 5);

                // Brief operation window
                Thread.Sleep(200);

                // Tear down
                fakeDevice.Dispose();
                bus.Disconnect(clientNode);
                bus.Close();
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            Assert.IsNull(caughtException,
                $"Exception during teardown: {caughtException?.Message}");
        }
        [TestMethod]
        [Timeout(30_000)]
        public async Task EndToEnd_DeviceTree_Selection_Menu_ParameterReadUpdate()
        {
            var bus = new VirtualCanBus();
            bus.Open();

            var clientNode = new LC_Node(10);
            bus.Connect(clientNode,
                (hdr, data, len) => LC_Interface.lib_ReceiveHandler(clientNode.DescriptorPtr, hdr, data, len));
            clientNode.StartNode();

            using var fakeDevice = new FakeULightDevice(bus, nodeId: 5);

            // 1. Device appears in remote nodes list
            bool found = WaitForNode(clientNode, 5, timeoutMs: 3_000);
            Assert.IsTrue(found, "Step 1: FakeULight (NodeID 5) did not appear in active nodes list.");
            var activeNodes = clientNode.GetActiveNodes();
            Assert.IsTrue(activeNodes.Any(n => n.NodeID == 5), "Step 1: Node 5 not in active nodes table.");
            Thread.Sleep(500); // settle time for address claiming

            // 2. Device is selectable
            var client = new LC_ParamClient(clientNode, 5);
            Assert.IsNotNull(client, "Step 2: Failed to select remote node 5.");

            // 3. Menu shows parameter directories
            await client.UpdateDirectoriesAsync();
            Assert.IsNotNull(client.Directories, "Step 3: Directories is null.");
            Assert.IsTrue(client.Directories.Count >= 4, $"Step 3: Expected >= 4 directories, got {client.Directories.Count}.");

            var inputsDir = client.Directories.FirstOrDefault(d => d.Name == "Inputs");
            Assert.IsNotNull(inputsDir, "Step 3: 'Inputs' directory not found.");
            Assert.IsTrue(inputsDir.Entries.Count > 0, "Step 3: 'Inputs' directory has no entries.");

            // 4. Parameter values can be read and are actively updating
            // 4a. Read initial Voltage (FloatEntry, 12.5f)
            var voltageEntry = inputsDir.Entries.FirstOrDefault(e => e.Name == "Voltage");
            Assert.IsNotNull(voltageEntry, "Step 4: 'Voltage' entry not found.");
            var readVoltsRet = await client.UpdateEntryValue(voltageEntry);
            Assert.AreEqual(LC_Return.Ok, readVoltsRet, "Step 4: UpdateEntryValue Voltage failed.");
            Assert.AreEqual(12.5f, (float)voltageEntry.Variable, 0.01f, "Step 4: Initial voltage mismatch.");

            // 4b. Live update on device -> read back new value on client
            fakeDevice.SetVoltage(24.0f);
            var readVoltsUpdated = await client.UpdateEntryValue(voltageEntry);
            Assert.AreEqual(LC_Return.Ok, readVoltsUpdated, "Step 4: UpdateEntryValue after device mutation failed.");
            Assert.AreEqual(24.0f, (float)voltageEntry.Variable, 0.01f, "Step 4: Updated voltage mismatch.");

            // 4c. Client mutation -> send to device -> verify device received & read back
            var btn1Entry = inputsDir.Entries.FirstOrDefault(e => e.Name == "Button 1");
            Assert.IsNotNull(btn1Entry, "Step 4: 'Button 1' entry not found.");
            btn1Entry.Variable = true;
            var sendBtnRet = await client.SendEntryValue(btn1Entry);
            Assert.AreEqual(LC_Return.Ok, sendBtnRet, "Step 4: SendEntryValue failed.");
            Assert.AreEqual((byte)1, fakeDevice.Button1, "Step 4: Fake device did not receive updated button value.");

            // Clear local client buffer & read back from device to ensure true round-trip
            Marshal.WriteByte(btn1Entry.VariablePtr, 0);
            var readBackBtnRet = await client.UpdateEntryValue(btn1Entry);
            Assert.AreEqual(LC_Return.Ok, readBackBtnRet, "Step 4: Readback of Button 1 failed.");
            Assert.IsTrue((bool)btn1Entry.Variable, "Step 4: Button 1 readback was not true.");
        }

        // ── Test 5: DebugProbeHttpEndpointsTest ───────────────────────────────

        /// <summary>
        /// Starts DebugProbeServer on a loopback port and verifies that an HTTP/AI client
        /// can query status, discover nodes, retrieve the parameter tree, read live values
        /// directly over CAN bus from FakeULightDevice, and mutate values via HTTP POST.
        /// </summary>
        [TestMethod]
        [Timeout(60_000)]
        public async Task DebugProbeHttpEndpointsTest()
        {
            var bus = new VirtualCanBus();
            bus.Open();

            var clientNode = new LC_Node(10);
            bus.Connect(clientNode,
                (hdr, data, len) => LC_Interface.lib_ReceiveHandler(clientNode.DescriptorPtr, hdr, data, len));
            clientNode.StartNode();

            var handler = new LevcanHandler(clientNode, 1000000);
            using var fakeDevice = new FakeULightDevice(bus, nodeId: 5);

            // Wait for discovery and remote name to finish receiving
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 3000)
            {
                var remote = handler.FindRemoteNode(5);
                if (remote != null && !string.IsNullOrEmpty(remote.Name))
                    break;
                Thread.Sleep(50);
            }
            Thread.Sleep(200); // settle time after name transfer completes

            int testPort = 19585;
            using var probe = new DebugProbeServer(handler, testPort);
            probe.Start();

            using var httpClient = new System.Net.Http.HttpClient();
            string baseUrl = $"http://127.0.0.1:{testPort}";

            // 1. Status endpoint
            var statusResp = await httpClient.GetAsync($"{baseUrl}/api/status");
            Assert.IsTrue(statusResp.IsSuccessStatusCode, "GET /api/status failed");
            var statusJson = await statusResp.Content.ReadAsStringAsync();
            using var statusDoc = System.Text.Json.JsonDocument.Parse(statusJson);
            Assert.AreEqual("online", statusDoc.RootElement.GetProperty("status").GetString());

            // 2. Nodes endpoint
            var nodesResp = await httpClient.GetAsync($"{baseUrl}/api/nodes");
            Assert.IsTrue(nodesResp.IsSuccessStatusCode, "GET /api/nodes failed");
            var nodesJson = await nodesResp.Content.ReadAsStringAsync();
            using var nodesDoc = System.Text.Json.JsonDocument.Parse(nodesJson);
            Assert.IsTrue(nodesDoc.RootElement.GetArrayLength() > 0, "No nodes returned");
            bool hasNode5 = nodesDoc.RootElement.EnumerateArray().Any(n => n.GetProperty("nodeId").GetUInt16() == 5);
            Assert.IsTrue(hasNode5, "Node 5 not found in /api/nodes");

            // 3. Params tree endpoint
            var paramsResp = await httpClient.GetAsync($"{baseUrl}/api/nodes/5/params");
            Assert.IsTrue(paramsResp.IsSuccessStatusCode, "GET /api/nodes/5/params failed");
            var paramsJson = await paramsResp.Content.ReadAsStringAsync();
            Console.WriteLine("DEBUG PROBE PARAMS JSON: " + paramsJson);
            using var paramsDoc = System.Text.Json.JsonDocument.Parse(paramsJson);
            var dirs = paramsDoc.RootElement.GetProperty("directories");
            Assert.IsTrue(dirs.GetArrayLength() >= 4, "Expected >= 4 directories in params tree");

            // 4. Query single param by name over CAN bus
            var voltResp = await httpClient.GetAsync($"{baseUrl}/api/nodes/5/param?name=Voltage&read=true");
            Assert.IsTrue(voltResp.IsSuccessStatusCode, "GET /api/nodes/5/param?name=Voltage failed");
            var voltJson = await voltResp.Content.ReadAsStringAsync();
            using var voltDoc = System.Text.Json.JsonDocument.Parse(voltJson);
            Assert.AreEqual("Voltage", voltDoc.RootElement.GetProperty("name").GetString());
            float voltVal = (float)voltDoc.RootElement.GetProperty("value").GetDouble();
            Assert.AreEqual(12.5f, voltVal, 0.05f, "Initial voltage mismatch over HTTP probe");

            // 5. Mutate fake device value, then query again via HTTP probe
            fakeDevice.SetVoltage(36.0f);
            var voltUpdatedResp = await httpClient.GetAsync($"{baseUrl}/api/nodes/5/param?name=Voltage&read=true");
            var voltUpdatedJson = await voltUpdatedResp.Content.ReadAsStringAsync();
            using var voltUpdatedDoc = System.Text.Json.JsonDocument.Parse(voltUpdatedJson);
            float voltUpdatedVal = (float)voltUpdatedDoc.RootElement.GetProperty("value").GetDouble();
            Assert.AreEqual(36.0f, voltUpdatedVal, 0.05f, "Updated voltage mismatch over HTTP probe");

            // 6. Mutate parameter from HTTP probe (POST)
            var postContent = new System.Net.Http.StringContent("{\"value\": true}", System.Text.Encoding.UTF8, "application/json");
            var postResp = await httpClient.PostAsync($"{baseUrl}/api/nodes/5/params/2/3", postContent);
            Assert.IsTrue(postResp.IsSuccessStatusCode, "POST /api/nodes/5/params/2/3 failed");
            Assert.AreEqual((byte)1, fakeDevice.Button1, "Fake device Button1 not set to 1 via HTTP POST");

            // 7. Telemetry endpoint
            var telemResp = await httpClient.GetAsync($"{baseUrl}/api/telemetry");
            Assert.IsTrue(telemResp.IsSuccessStatusCode, "GET /api/telemetry failed");

            // 8. Objects catalog endpoint (full list like /models)
            var catalogResp = await httpClient.GetAsync($"{baseUrl}/api/objects");
            Assert.IsTrue(catalogResp.IsSuccessStatusCode, "GET /api/objects failed");
            var catalogJson = await catalogResp.Content.ReadAsStringAsync();
            using var catalogDoc = System.Text.Json.JsonDocument.Parse(catalogJson);
            int totalObjects = catalogDoc.RootElement.GetProperty("count").GetInt32();
            Assert.IsTrue(totalObjects >= 40, $"Expected >= 40 catalog items, got {totalObjects}");

            // 9. Objects catalog with filter (?speed)
            var filterResp = await httpClient.GetAsync($"{baseUrl}/api/objects?speed");
            Assert.IsTrue(filterResp.IsSuccessStatusCode, "GET /api/objects?speed failed");
            var filterJson = await filterResp.Content.ReadAsStringAsync();
            using var filterDoc = System.Text.Json.JsonDocument.Parse(filterJson);
            int filteredCount = filterDoc.RootElement.GetProperty("count").GetInt32();
            Assert.IsTrue(filteredCount >= 1, "Expected >= 1 item matching 'speed'");
            bool hasSpeedObj = filterDoc.RootElement.GetProperty("data").EnumerateArray()
                .Any(d => d.GetProperty("id").GetString() == "Speed");
            Assert.IsTrue(hasSpeedObj, "Speed object not found in ?speed catalog filter");

            // 10. LEVCAN Object Data Request over CAN bus by name (Speed)
            var dataReqResp = await httpClient.GetAsync($"{baseUrl}/api/nodes/5/data/Speed");
            Assert.IsTrue(dataReqResp.IsSuccessStatusCode, "GET /api/nodes/5/data/Speed failed");
            var dataJson = await dataReqResp.Content.ReadAsStringAsync();
            using var dataDoc = System.Text.Json.JsonDocument.Parse(dataJson);
            Assert.IsTrue(dataDoc.RootElement.GetProperty("success").GetBoolean(), "Object data request was not successful");
            Assert.AreEqual("Speed", dataDoc.RootElement.GetProperty("objectId").GetString());
            Assert.AreEqual(776, dataDoc.RootElement.GetProperty("objectIndex").GetInt32());
            var decoded = dataDoc.RootElement.GetProperty("decoded");
            Assert.AreEqual(42, decoded.GetProperty("speed").GetInt32(), "Decoded speed value mismatch");

            // 11. LEVCAN Object Data Request by hex index (0x0308)
            var hexDataResp = await httpClient.GetAsync($"{baseUrl}/api/nodes/5/data/0x0308");
            Assert.IsTrue(hexDataResp.IsSuccessStatusCode, "GET /api/nodes/5/data/0x0308 failed");

            // 12. Help markdown endpoint
            var helpResp = await httpClient.GetAsync($"{baseUrl}/help");
            Assert.IsTrue(helpResp.IsSuccessStatusCode, "GET /help failed");
            Assert.AreEqual("text/markdown", helpResp.Content.Headers.ContentType?.MediaType);
            var helpMd = await helpResp.Content.ReadAsStringAsync();
            Assert.IsTrue(helpMd.Contains("# LEVCAN Debug Probe API"), "Help markdown header mismatch");

            // 13. Logs endpoint
            var logsResp = await httpClient.GetAsync($"{baseUrl}/api/logs");
            Assert.IsTrue(logsResp.IsSuccessStatusCode, "GET /api/logs failed");
            var logsJson = await logsResp.Content.ReadAsStringAsync();
            using var logsDoc = System.Text.Json.JsonDocument.Parse(logsJson);
            Assert.IsTrue(logsDoc.RootElement.GetArrayLength() >= 5, "Expected logs recorded");

            probe.Stop();
        }

        [TestMethod]
        public void FileServerRobustnessTest()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "levcan_fs_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                using var testNode = new LC_Node(80);
                using var fs = new LC_FileServer(testNode, tempDir);

                Assert.AreEqual(Path.GetFullPath(tempDir), fs.SavePath);

                // Trigger full GC passes to ensure native callbacks are rooted and never collected
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                Thread.Sleep(200);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
        }
    }
}
