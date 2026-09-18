using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LEVCAN;

namespace LEVCAN_Configurator_Shared
{
    public class ProbeRequestLog
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Method { get; set; } = "";
        public string Path { get; set; } = "";
        public int StatusCode { get; set; }
        public double DurationMs { get; set; }
        public string? ClientIp { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class DebugProbeServer : IDisposable
    {
        private HttpListener? _listener;
        private CancellationTokenSource? _cts;
        private Task? _listenerLoop;
        private readonly LevcanHandler _lev;
        private int _port;
        private readonly DateTime _startTime = DateTime.UtcNow;
        private readonly List<ProbeRequestLog> _logs = new();
        private readonly object _logsLock = new();
        private const int MaxLogs = 100;

        public bool IsRunning => _listener?.IsListening ?? false;
        public int Port => _port;
        public string BaseUrl => $"http://127.0.0.1:{_port}/";

        public event Action<ProbeRequestLog>? RequestLogged;
        public event Action<bool>? StateChanged;

        public DebugProbeServer(LevcanHandler lev, int port = 8585)
        {
            _lev = lev;
            _port = port;
        }

        public void Start(int? newPort = null)
        {
            if (newPort.HasValue && newPort.Value > 0)
                _port = newPort.Value;

            if (_listener != null && _listener.IsListening)
                Stop();

            _cts = new CancellationTokenSource();
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://127.0.0.1:{_port}/");
            _listener.Prefixes.Add($"http://localhost:{_port}/");

            try
            {
                _listener.Start();
                _listenerLoop = Task.Run(() => ListenLoopAsync(_cts.Token));
                StateChanged?.Invoke(true);
            }
            catch (Exception ex)
            {
                AddLog(new ProbeRequestLog
                {
                    Method = "STARTUP",
                    Path = BaseUrl,
                    StatusCode = 500,
                    ErrorMessage = ex.Message
                });
                Stop();
                throw;
            }
        }

        public void Stop()
        {
            try
            {
                _cts?.Cancel();
                if (_listener != null)
                {
                    if (_listener.IsListening)
                        _listener.Stop();
                    _listener.Close();
                    _listener = null;
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
            finally
            {
                StateChanged?.Invoke(false);
            }
        }

        public List<ProbeRequestLog> GetRecentLogs()
        {
            lock (_logsLock)
            {
                return new List<ProbeRequestLog>(_logs);
            }
        }

        private void AddLog(ProbeRequestLog log)
        {
            lock (_logsLock)
            {
                _logs.Add(log);
                if (_logs.Count > MaxLogs)
                    _logs.RemoveAt(0);
            }
            RequestLogged?.Invoke(log);
        }

        private async Task ListenLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && _listener != null && _listener.IsListening)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = Task.Run(() => HandleRequestAsync(context, token), token);
                }
                catch (HttpListenerException) when (token.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (token.IsCancellationRequested) break;
                    AddLog(new ProbeRequestLog
                    {
                        Method = "INTERNAL",
                        Path = "/",
                        StatusCode = 500,
                        ErrorMessage = ex.Message
                    });
                }
            }
        }

        private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken token)
        {
            var sw = Stopwatch.StartNew();
            var req = context.Request;
            var res = context.Response;
            var rawPath = req.Url?.AbsolutePath ?? "/";
            var path = rawPath.TrimEnd('/');
            if (string.IsNullOrEmpty(path)) path = "/";
            var method = req.HttpMethod.ToUpperInvariant();
            var clientIp = req.RemoteEndPoint?.Address.ToString();

            // Set CORS headers for all responses
            res.Headers.Add("Access-Control-Allow-Origin", "*");
            res.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            res.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");

            if (method == "OPTIONS")
            {
                res.StatusCode = 204;
                res.Close();
                return;
            }

            try
            {
                if (path == "/help" || path == "/api/help")
                {
                    await WriteMarkdownAsync(res, HelpMarkdown);
                }
                else if (path == "" || path == "/" || path == "/api" || path == "/api/status")
                {
                    await HandleStatusAsync(res);
                }
                else if (path == "/api/nodes")
                {
                    await HandleNodesAsync(res);
                }
                else if (path == "/api/telemetry")
                {
                    await HandleTelemetryAsync(req, res);
                }
                else if (path == "/api/telemetry/stream")
                {
                    await HandleTelemetryStreamAsync(req, res, token);
                    return; // Stream closed inside
                }
                else if (path == "/api/logs")
                {
                    await WriteJsonAsync(res, GetRecentLogs());
                }
                else if (path == "/api/objects" || path == "/api/data/objects" || path == "/api/models" || path == "/v1/models")
                {
                    await HandleObjectsCatalogAsync(req, res);
                }
                else if (path.StartsWith("/api/nodes/"))
                {
                    await HandleNodeSubpathAsync(path, method, req, res, token);
                }
                else
                {
                    res.StatusCode = 404;
                    await WriteJsonAsync(res, new { error = "Not found", path }, 404);
                }

                sw.Stop();
                AddLog(new ProbeRequestLog
                {
                    Method = method,
                    Path = rawPath,
                    StatusCode = res.StatusCode,
                    DurationMs = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
                    ClientIp = clientIp
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                res.StatusCode = 500;
                await WriteJsonAsync(res, new { error = ex.Message, stack = ex.StackTrace }, 500);
                AddLog(new ProbeRequestLog
                {
                    Method = method,
                    Path = rawPath,
                    StatusCode = 500,
                    DurationMs = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
                    ClientIp = clientIp,
                    ErrorMessage = ex.Message
                });
            }
            finally
            {
                try { res.Close(); } catch { }
            }
        }

        private async Task HandleStatusAsync(HttpListenerResponse res)
        {
            var nodes = _lev.GetRemoteNodesCopy();
            var status = new
            {
                status = "online",
                probe = "LEVCAN Debug Probe",
                version = "1.0",
                port = _port,
                device = _lev.CurrentDevice.ToString(),
                baudrate = _lev.Baudrate,
                myNodeId = _lev.Node.ShortName.NodeID,
                uptimeSeconds = (int)(DateTime.UtcNow - _startTime).TotalSeconds,
                nodesCount = nodes.Count,
                nodes = nodes.Select(n => new
                {
                    nodeId = n.ShortName.NodeID,
                    name = n.Name,
                    configurable = n.ShortName.Configurable
                })
            };
            await WriteJsonAsync(res, status);
        }

        private async Task HandleNodesAsync(HttpListenerResponse res)
        {
            var remotes = _lev.GetRemoteNodesCopy();
            var active = _lev.Node.GetActiveNodes();
            var list = new List<object>();

            foreach (var r in remotes)
            {
                list.Add(new
                {
                    nodeId = r.ShortName.NodeID,
                    name = r.Name ?? "",
                    deviceType = ((LC_Device)r.ShortName.DeviceType).ToString(),
                    configurable = r.ShortName.Configurable,
                    codePage = r.ShortName.CodePage?.HeaderName ?? "unknown"
                });
            }

            if (active != null)
            {
                foreach (var a in active)
                {
                    if (a.NodeID < (ushort)LC_Address.Null && a.NodeID != _lev.Node.ShortName.NodeID && !remotes.Any(r => r.ShortName.NodeID == a.NodeID))
                    {
                        list.Add(new
                        {
                            nodeId = a.NodeID,
                            name = "",
                            deviceType = ((LC_Device)a.DeviceType).ToString(),
                            configurable = a.Configurable,
                            codePage = a.CodePage?.HeaderName ?? "unknown"
                        });
                    }
                }
            }

            await WriteJsonAsync(res, list);
        }

        private async Task HandleTelemetryAsync(HttpListenerRequest req, HttpListenerResponse res)
        {
            var nodeQuery = req.QueryString["node"] ?? req.QueryString["nodeId"];
            var requestFresh = req.QueryString["request"] == "true";

            if (!string.IsNullOrEmpty(nodeQuery) && ushort.TryParse(nodeQuery, out var targetId))
            {
                if (requestFresh)
                    _lev.Telemetry.RequestTelemetry(_lev.Node, targetId);

                var telem = _lev.Telemetry.Get(targetId);
                if (telem == null)
                {
                    await WriteJsonAsync(res, new { nodeId = targetId, status = "no_telemetry_received_yet" });
                }
                else
                {
                    await WriteJsonAsync(res, telem.ToSummaryDictionary());
                }
            }
            else
            {
                var all = _lev.Telemetry.AllTelemetry.Values.Select(t => t.ToSummaryDictionary()).ToList();
                await WriteJsonAsync(res, all);
            }
        }

        private async Task HandleTelemetryStreamAsync(HttpListenerRequest req, HttpListenerResponse res, CancellationToken parentToken)
        {
            res.ContentType = "text/event-stream";
            res.Headers.Add("Cache-Control", "no-cache");
            res.Headers.Add("Connection", "keep-alive");
            res.StatusCode = 200;

            var nodeQuery = req.QueryString["node"] ?? req.QueryString["nodeId"];
            ushort? filterNodeId = null;
            if (!string.IsNullOrEmpty(nodeQuery) && ushort.TryParse(nodeQuery, out var nId))
                filterNodeId = nId;

            using var outputStream = res.OutputStream;
            using var writer = new StreamWriter(outputStream, new UTF8Encoding(false)) { AutoFlush = true };

            // Channel/queue for push notifications
            var channel = new BlockingCollection<string>(boundedCapacity: 200);

            void OnTelemetry(ushort nId, NodeTelemetry telem)
            {
                if (filterNodeId.HasValue && filterNodeId.Value != nId) return;
                var json = JsonSerializer.Serialize(telem.ToSummaryDictionary());
                channel.TryAdd($"event: telemetry\ndata: {json}\n\n");
            }

            _lev.Telemetry.TelemetryUpdated += OnTelemetry;

            // Push initial snapshot if available
            if (filterNodeId.HasValue)
            {
                var snap = _lev.Telemetry.Get(filterNodeId.Value);
                if (snap != null)
                    OnTelemetry(filterNodeId.Value, snap);
            }
            else
            {
                foreach (var snap in _lev.Telemetry.AllTelemetry.Values)
                    OnTelemetry(snap.NodeId, snap);
            }

            try
            {
                while (!parentToken.IsCancellationRequested)
                {
                    if (channel.TryTake(out var message, 2000))
                    {
                        await writer.WriteAsync(message);
                    }
                    else
                    {
                        // Heartbeat ping
                        await writer.WriteAsync(": ping\n\n");
                    }
                }
            }
            catch
            {
                // Client disconnected
            }
            finally
            {
                _lev.Telemetry.TelemetryUpdated -= OnTelemetry;
                channel.Dispose();
            }
        }

        private async Task HandleNodeSubpathAsync(string path, string method, HttpListenerRequest req, HttpListenerResponse res, CancellationToken token = default)
        {
            // /api/nodes/{id}/...
            var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3 || !ushort.TryParse(parts[2], out var nodeId))
            {
                res.StatusCode = 400;
                await WriteJsonAsync(res, new { error = "Invalid node ID in URL path" }, 400);
                return;
            }

            var remote = _lev.FindRemoteNode(nodeId);
            if (remote == null)
            {
                var active = _lev.Node.GetActiveNodes();
                var match = active?.FirstOrDefault(a => a.NodeID == nodeId);
                if (match.HasValue && match.Value.NodeID == nodeId && nodeId < (ushort)LC_Address.Null)
                {
                    remote = new LCRemoteNode(match.Value);
                }
            }

            if (remote == null)
            {
                res.StatusCode = 404;
                await WriteJsonAsync(res, new { error = $"Node {nodeId} not found on CAN bus" }, 404);
                return;
            }

            var subAction = parts.Length > 3 ? parts[3].ToLowerInvariant() : "";

            if (subAction == "telemetry")
            {
                var telem = _lev.Telemetry.Get(nodeId);
                if (req.QueryString["request"] == "true")
                    _lev.Telemetry.RequestTelemetry(_lev.Node, nodeId);

                if (telem == null)
                    await WriteJsonAsync(res, new { nodeId, status = "no_telemetry_received_yet" });
                else
                    await WriteJsonAsync(res, telem.ToSummaryDictionary());
            }
            else if (subAction == "params" || subAction == "tree")
            {
                await HandleParamsTreeAsync(nodeId, remote, parts, method, req, res);
            }
            else if (subAction == "param")
            {
                await HandleSingleParamByNameOrPathAsync(nodeId, remote, req, res);
            }
            else if (subAction == "data" || subAction == "request" || subAction == "obj" || subAction == "object")
            {
                await HandleObjectDataRequestAsync(nodeId, remote, parts, req, res, token);
            }
            else if (subAction == "update" || subAction == "force-update")
            {
                // Send force-update command { 0xDA, 0xCE, 0xCA, 0x02 } to node
                var ret = _lev.Node.SendData(new byte[] { 0xDA, 0xCE, 0xCA, 0x02 }, (byte)nodeId, (ushort)LC_SystemMessage.SWUpdate);
                await WriteJsonAsync(res, new
                {
                    nodeId,
                    command = "ForceUpdate",
                    status = ret.ToString(),
                    message = "Sent SWUpdate force update command to node"
                });
            }
            else
            {
                res.StatusCode = 404;
                await WriteJsonAsync(res, new { error = $"Unknown subaction '{subAction}' for node {nodeId}" }, 404);
            }
        }

        private async Task HandleObjectsCatalogAsync(HttpListenerRequest req, HttpListenerResponse res)
        {
            string? query = req.QueryString["filter"] ?? req.QueryString["query"] ?? req.QueryString["q"] ?? req.QueryString["search"];

            if (string.IsNullOrEmpty(query))
            {
                // Support flags in query string like ?speed or ?voltage
                var rawQuery = req.Url?.Query;
                if (!string.IsNullOrEmpty(rawQuery))
                {
                    string trimmed = rawQuery.TrimStart('?');
                    int amp = trimmed.IndexOf('&');
                    if (amp >= 0) trimmed = trimmed.Substring(0, amp);
                    int eq = trimmed.IndexOf('=');
                    if (eq >= 0)
                    {
                        string k = trimmed.Substring(0, eq);
                        string v = trimmed.Substring(eq + 1);
                        query = string.IsNullOrEmpty(v) ? k : v;
                    }
                    else
                    {
                        query = trimmed;
                    }
                }
            }

            var filtered = LevcanObjectCatalog.Filter(query);
            var result = new
            {
                @object = "list",
                count = filtered.Count,
                filter = string.IsNullOrEmpty(query) ? null : query,
                data = filtered
            };

            await WriteJsonAsync(res, result);
        }

        private async Task HandleObjectDataRequestAsync(ushort nodeId, LCRemoteNode remote, string[] parts, HttpListenerRequest req, HttpListenerResponse res, CancellationToken token)
        {
            // /api/nodes/{id}/data/{objectId} or ?object=...
            string? objectIdStr = parts.Length > 4 ? parts[4] : (req.QueryString["object"] ?? req.QueryString["id"] ?? req.QueryString["name"]);

            if (string.IsNullOrWhiteSpace(objectIdStr))
            {
                res.StatusCode = 400;
                await WriteJsonAsync(res, new
                {
                    error = "Missing object identifier in path or query (e.g. /api/nodes/1/data/Speed or /api/nodes/1/data/0x0308). Use GET /api/objects to list available objects.",
                    catalogUrl = "/api/objects"
                }, 400);
                return;
            }

            var objInfo = LevcanObjectCatalog.Resolve(objectIdStr);
            if (objInfo == null)
            {
                res.StatusCode = 400;
                await WriteJsonAsync(res, new
                {
                    error = $"Unknown object identifier '{objectIdStr}'. Use GET /api/objects to view available standard objects, or specify a numerical index (0-1023 or 0x0000-0x03FF).",
                    catalogUrl = "/api/objects"
                }, 400);
                return;
            }

            int timeoutMs = 1500;
            if (int.TryParse(req.QueryString["timeout"], out var t) && t > 0)
                timeoutMs = Math.Min(t, 10000);

            var result = await _lev.RequestObjectDataAsync(nodeId, objInfo.Index, timeoutMs, token);
            if (!result.Success)
            {
                res.StatusCode = result.Error?.Contains("Timeout", StringComparison.OrdinalIgnoreCase) == true ? 504 : 400;
            }

            await WriteJsonAsync(res, result, res.StatusCode);
        }

        private async Task HandleParamsTreeAsync(ushort nodeId, LCRemoteNode remote, string[] parts, string method, HttpListenerRequest req, HttpListenerResponse res)
        {
            var client = _lev.GetParametersClient(remote);
            if (client == null)
            {
                res.StatusCode = 500;
                await WriteJsonAsync(res, new { error = $"Could not create LC_ParamClient for node {nodeId}" }, 500);
                return;
            }

            // Route 1: /api/nodes/{id}/params/{dir}/{index}
            if (parts.Length >= 6)
            {
                if (!ushort.TryParse(parts[4], out var dirIndex) || !ushort.TryParse(parts[5], out var entryIndex))
                {
                    res.StatusCode = 400;
                    await WriteJsonAsync(res, new { error = "Invalid directory or entry index in path" }, 400);
                    return;
                }

                await HandleSpecificEntryAsync(nodeId, client, dirIndex, entryIndex, method, req, res);
                return;
            }

            // Route 2: /api/nodes/{id}/params
            bool refresh = req.QueryString["refresh"] == "true" || client.Directories.Count == 0;
            bool readAll = req.QueryString["readAll"] == "true";

            if (refresh)
            {
                await client.UpdateDirectoriesAsync();
            }

            if (readAll && client.Directories != null)
            {
                foreach (var dir in client.Directories)
                {
                    if (dir.Entries == null) continue;
                    foreach (var entry in dir.Entries)
                    {
                        if (entry.VariablePtr != IntPtr.Zero)
                        {
                            try { await entry.UpdateVariable(); } catch { }
                        }
                    }
                }
            }

            var tree = SerializeDirectoryTree(nodeId, client.Directories ?? new List<LCPC_Directory>());
            await WriteJsonAsync(res, tree);
        }

        private async Task HandleSpecificEntryAsync(ushort nodeId, LC_ParamClient client, ushort dirIndex, ushort entryIndex, string method, HttpListenerRequest req, HttpListenerResponse res)
        {
            if (client.Directories == null || client.Directories.Count == 0)
                await client.UpdateDirectoriesAsync();

            var dir = client.Directories?.FirstOrDefault(d => d.Index == dirIndex);
            if (dir == null)
            {
                // Try requesting single directory
                dir = client.RequestDirectory(dirIndex);
                if (dir != null)
                    await client.UpdateEntriesAsync(dirIndex);
            }

            var entry = dir?.Entries?.FirstOrDefault(e => e.Index == entryIndex);
            if (entry == null)
            {
                res.StatusCode = 404;
                await WriteJsonAsync(res, new { error = $"Entry [{dirIndex}][{entryIndex}] not found on node {nodeId}" }, 404);
                return;
            }

            if (method == "GET")
            {
                bool readLive = req.QueryString["read"] != "false"; // default true
                LC_Return readStatus = LC_Return.Ok;
                if (readLive && entry.VariablePtr != IntPtr.Zero)
                {
                    readStatus = await client.UpdateEntryValue(entry);
                }

                var result = SerializeEntry(entry, includeDescriptor: true);
                result["status"] = readStatus.ToString();
                result["nodeId"] = nodeId;
                await WriteJsonAsync(res, result);
            }
            else if (method == "POST")
            {
                using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
                var body = await reader.ReadToEndAsync();
                object? parsedValue = null;

                try
                {
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("value", out var valProp))
                    {
                        parsedValue = ExtractJsonElementValue(valProp);
                    }
                    else
                    {
                        parsedValue = ExtractJsonElementValue(doc.RootElement);
                    }
                }
                catch (Exception ex)
                {
                    res.StatusCode = 400;
                    await WriteJsonAsync(res, new { error = $"Invalid JSON payload: {ex.Message}" }, 400);
                    return;
                }

                if (parsedValue == null)
                {
                    res.StatusCode = 400;
                    await WriteJsonAsync(res, new { error = "No 'value' provided in request body" }, 400);
                    return;
                }

                try
                {
                    SetEntryValue(entry, parsedValue);
                    var sendStatus = await client.SendEntryValue(entry);

                    var result = SerializeEntry(entry, includeDescriptor: true);
                    result["status"] = sendStatus.ToString();
                    result["nodeId"] = nodeId;
                    await WriteJsonAsync(res, result);
                }
                catch (Exception ex)
                {
                    res.StatusCode = 500;
                    await WriteJsonAsync(res, new { error = $"Failed to set parameter: {ex.Message}" }, 500);
                }
            }
            else
            {
                res.StatusCode = 405;
                await WriteJsonAsync(res, new { error = $"Method {method} not allowed" }, 405);
            }
        }

        private async Task HandleSingleParamByNameOrPathAsync(ushort nodeId, LCRemoteNode remote, HttpListenerRequest req, HttpListenerResponse res)
        {
            var name = req.QueryString["name"];
            var path = req.QueryString["path"];
            bool readLive = req.QueryString["read"] != "false";

            if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(path))
            {
                res.StatusCode = 400;
                await WriteJsonAsync(res, new { error = "Specify query parameter 'name' or 'path'" }, 400);
                return;
            }

            var client = _lev.GetParametersClient(remote);
            if (client == null)
            {
                res.StatusCode = 500;
                await WriteJsonAsync(res, new { error = $"Could not get LC_ParamClient for node {nodeId}" }, 500);
                return;
            }

            if (client.Directories == null || client.Directories.Count == 0)
                await client.UpdateDirectoriesAsync();

            LCPC_Entry? foundEntry = null;
            LCPC_Directory? foundDir = null;

            if (!string.IsNullOrEmpty(path))
            {
                // Path format: "Inputs/Voltage" or "Menu/Save"
                var parts = path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    foundDir = client.Directories?.FirstOrDefault(d => string.Equals(d.Name, parts[0], StringComparison.OrdinalIgnoreCase));
                    foundEntry = foundDir?.Entries?.FirstOrDefault(e => string.Equals(e.Name, parts[1], StringComparison.OrdinalIgnoreCase));
                }
                else if (parts.Length == 1)
                {
                    name = parts[0];
                }
            }

            if (foundEntry == null && !string.IsNullOrEmpty(name) && client.Directories != null)
            {
                foreach (var d in client.Directories)
                {
                    if (d.Entries == null) continue;
                    var match = d.Entries.FirstOrDefault(e =>
                        string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase) ||
                        (e.Name != null && e.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0));
                    if (match != null)
                    {
                        foundEntry = match;
                        foundDir = d;
                        break;
                    }
                }
            }

            if (foundEntry == null)
            {
                res.StatusCode = 404;
                await WriteJsonAsync(res, new { error = $"Parameter '{path ?? name}' not found on node {nodeId}" }, 404);
                return;
            }

            LC_Return readStatus = LC_Return.Ok;
            if (readLive && foundEntry.VariablePtr != IntPtr.Zero)
            {
                readStatus = await client.UpdateEntryValue(foundEntry);
            }

            var result = SerializeEntry(foundEntry, includeDescriptor: true);
            result["directoryName"] = foundDir?.Name;
            result["status"] = readStatus.ToString();
            result["nodeId"] = nodeId;
            await WriteJsonAsync(res, result);
        }

        private static object SerializeDirectoryTree(ushort nodeId, List<LCPC_Directory> directories)
        {
            return new
            {
                nodeId,
                directories = directories.Select(d => new
                {
                    index = d.Index,
                    name = d.Name ?? $"Dir_{d.Index}",
                    entries = d.Entries?.Select(e => SerializeEntry(e, includeDescriptor: true)).ToList()
                }).ToList()
            };
        }

        private static Dictionary<string, object?> SerializeEntry(LCPC_Entry entry, bool includeDescriptor = false)
        {
            var dict = new Dictionary<string, object?>
            {
                ["name"] = entry.Name,
                ["index"] = entry.Index,
                ["parentDirectory"] = entry.ParentDirectory,
                ["type"] = entry.EType.ToString(),
                ["mode"] = entry.Mode.ToString(),
                ["value"] = SafeGetVariable(entry),
                ["textData"] = entry.TextData
            };

            if (entry.EType == LCP_EntryType.Folder)
            {
                dict["folderDirIndex"] = entry.FolderDirIndex;
            }

            if (includeDescriptor && entry.Descriptor != null)
            {
                dict["descriptor"] = SerializeDescriptor(entry);
            }

            return dict;
        }

        private static object? SerializeDescriptor(LCPC_Entry entry)
        {
            var d = entry.Descriptor;
            if (d == null) return null;

            if (d is LCP_Float f)
                return new { min = f.Min, max = f.Max, step = f.Step };
            if (d is LCP_Int32 i)
                return new { min = i.Min, max = i.Max, step = i.Step };
            if (d is LCP_Uint32 u)
                return new { min = u.Min, max = u.Max, step = u.Step };
            if (d is LCP_Decimal32 dec)
                return new { min = dec.Min, max = dec.Max, step = dec.Step, decimals = dec.Decimals };
            if (d is LCP_Enum en)
                return new { min = en.Min, size = en.Size, labels = entry.TextDataAsArray };

            return d.ToString();
        }

        private static object? SafeGetVariable(LCPC_Entry entry)
        {
            try
            {
                if (entry.VariablePtr == IntPtr.Zero || entry.EType == LCP_EntryType.Folder)
                    return null;
                return entry.Variable;
            }
            catch
            {
                return null;
            }
        }

        private static void SetEntryValue(LCPC_Entry entry, object value)
        {
            switch (entry.EType)
            {
                case LCP_EntryType.Float:
                    entry.Variable = Convert.ToSingle(value);
                    break;
                case LCP_EntryType.Double:
                    entry.Variable = Convert.ToDouble(value);
                    break;
                case LCP_EntryType.Bool:
                    entry.Variable = Convert.ToBoolean(value);
                    break;
                case LCP_EntryType.Int32:
                    entry.Variable = Convert.ToInt32(value);
                    break;
                case LCP_EntryType.Uint32:
                    entry.Variable = Convert.ToUInt32(value);
                    break;
                case LCP_EntryType.Int64:
                    entry.Variable = Convert.ToInt64(value);
                    break;
                case LCP_EntryType.Uint64:
                    entry.Variable = Convert.ToUInt64(value);
                    break;
                case LCP_EntryType.Enum:
                    entry.Variable = Convert.ToUInt32(value);
                    break;
                case LCP_EntryType.Decimal32:
                    // If descriptor has Decimals and value is floating point string/number
                    if (entry.Descriptor is LCP_Decimal32 decDesc && decDesc.Decimals > 0)
                    {
                        double dVal = Convert.ToDouble(value);
                        long rawFixed = (long)Math.Round(dVal * Math.Pow(10, decDesc.Decimals));
                        entry.Variable = (int)rawFixed;
                    }
                    else
                    {
                        entry.Variable = Convert.ToInt32(value);
                    }
                    break;
                case LCP_EntryType.String:
                    entry.Variable = value?.ToString() ?? "";
                    break;
                default:
                    entry.Variable = value;
                    break;
            }
        }

        private static object? ExtractJsonElementValue(JsonElement el)
        {
            return el.ValueKind switch
            {
                JsonValueKind.Number => el.TryGetInt64(out var l) ? (object)l : el.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String => el.GetString(),
                _ => null
            };
        }

        private static async Task WriteMarkdownAsync(HttpListenerResponse res, string markdown, int statusCode = 200)
        {
            res.ContentType = "text/markdown; charset=utf-8";
            res.StatusCode = statusCode;
            var bytes = Encoding.UTF8.GetBytes(markdown);
            res.ContentLength64 = bytes.Length;
            await res.OutputStream.WriteAsync(bytes, 0, bytes.Length);
        }

        private static async Task WriteJsonAsync(HttpListenerResponse res, object data, int statusCode = 200)
        {
            res.ContentType = "application/json; charset=utf-8";
            res.StatusCode = statusCode;
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var bytes = Encoding.UTF8.GetBytes(json);
            res.ContentLength64 = bytes.Length;
            await res.OutputStream.WriteAsync(bytes, 0, bytes.Length);
        }

        private const string HelpMarkdown = @"# LEVCAN Debug Probe API

Local HTTP loopback interface for inspecting and interacting with LEVCAN devices over CAN bus.

## 1. Status & Devices
- `GET /api/status` — Probe and CAN bus status, active CAN adapter, connected node count.
- `GET /api/nodes` — List all discovered nodes on the bus (Node ID, name, device type).

## 2. Telemetry
- `GET /api/telemetry` — Live snapshot of speed/RPM, voltages, currents, temperatures.
- `GET /api/nodes/{id}/telemetry` — Telemetry snapshot for specific node.
- `GET /api/telemetry/stream` — Real-time Server-Sent Events (SSE) telemetry stream (`curl -N`).

## 3. Parameters (Device Tree)
- `GET /api/nodes/{id}/params` — Full parameter tree (directories, entries, descriptors).
- `GET /api/nodes/{id}/param?name={Name}&read=true` — Read specific parameter live over CAN.
- `GET /api/nodes/{id}/params/{dir}/{index}?read=true` — Read parameter by directory and index.
- `POST /api/nodes/{id}/params/{dir}/{index}` — Set parameter (`{""value"": ...}`).

## 4. LEVCAN Object Data Requests
- `GET /api/objects` — Catalog of all requestable objects (clean ID, hex, raw name, struct).
- `GET /api/objects?{filter}` — Filter catalog (e.g. `?speed`, `?temp`, `?voltage`, `?filter=supply`).
- `GET /v1/models` or `GET /api/models` — OpenAI / llama.cpp-style model catalog aliases.
- `GET /api/nodes/{id}/data/{objectId}` — Send active CAN request and return data + decoded struct.
  - `{objectId}` accepts clean name (`Speed`, `DCSupply`, `Temperature`), raw name (`LC_Obj_Speed`), decimal (`776`), or hex (`0x0308`).
  - Optional `?timeout=1500` (ms).

## 5. Firmware & Control Actions
- `POST /api/nodes/{id}/force-update` or `POST /api/nodes/{id}/update` — Trigger controller bootloader firmware update over CAN (`LC_SystemMessage.SWUpdate` with `{ 0xDA, 0xCE, 0xCA, 0x02 }`).

## 6. System & Logs
- `GET /api/logs` — Recent incoming requests, status codes, and latency in ms.
- `GET /help` or `GET /api/help` — This help documentation (markdown).
";

        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
        }
    }
}
