using CueStrike.MCP.Tools;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;

using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace CueStrike.MCP
{
    /// <summary>
    /// MCP HTTP Server for Unity Editor.
    /// Implements JSON-RPC 2.0 over HTTP for Model Context Protocol communication.
    /// </summary>
    [InitializeOnLoad]
    public static class McpServer
    {
        private static HttpListener _listener;
        private static Thread _serverThread;
        private static CancellationTokenSource _cancellationToken;
        private static bool _isRunning = false;
        private static readonly Dictionary<string, IMcpTool> _tools = new();
        private static McpSettings _settings;

        /// <summary>
        /// Event fired when the server logs a message.
        /// </summary>
        public static event Action<string> OnLog;

        static McpServer()
        {
            // Register tools
            RegisterTool(new ExecuteCodeTool());
            RegisterTool(new ReadFileTool());
            RegisterTool(new WriteFileTool());
            RegisterTool(new ListFilesTool());
            RegisterTool(new SearchFilesTool());

            // Load settings
            LoadSettings();

            // Auto-start if configured
            EditorApplication.delayCall += () =>
            {
                if (_settings != null && _settings.port > 0)
                {
                    StartServer(_settings.port, _settings.requireAuth, _settings.authToken);
                }
            };
        }

        /// <summary>
        /// Gets whether the server is currently running.
        /// </summary>
        public static bool IsRunning => _isRunning;

        /// <summary>
        /// Gets the server port.
        /// </summary>
        public static int Port { get; private set; }

        /// <summary>
        /// Gets the server URL.
        /// </summary>
        public static string ServerUrl => $"http://localhost:{Port}/";

        /// <summary>
        /// Registers an MCP tool.
        /// </summary>
        public static void RegisterTool(IMcpTool tool)
        {
            if (tool == null) return;
            _tools[tool.Name] = tool;
            UnityEngine.Debug.Log($"[MCP] Registered tool: {tool.Name}");
        }

        /// <summary>
        /// Starts the MCP HTTP server.
        /// </summary>
        public static bool StartServer(int port = 8080, bool requireAuth = false, string authToken = "")
        {
            if (_isRunning)
            {
                UnityEngine.Debug.LogWarning("[MCP] Server is already running");
                return false;
            }

            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://localhost:{port}/");
                _listener.Start();

                _cancellationToken = new CancellationTokenSource();
                _serverThread = new Thread(() => RunServerLoop(_cancellationToken.Token));
                _serverThread.IsBackground = true;
                _serverThread.Start();

                Port = port;
                _isRunning = true;

                UnityEngine.Debug.Log($"[MCP] Server started at http://localhost:{port}/");
                return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[MCP] Failed to start server: {ex.Message}");
                StopServer();
                return false;
            }
        }

        /// <summary>
        /// Stops the MCP HTTP server.
        /// </summary>
        public static void StopServer()
        {
            if (!_isRunning) return;

            _cancellationToken?.Cancel();
            _listener?.Stop();
            _listener?.Close();
            _serverThread?.Join(1000);

            _isRunning = false;
            UnityEngine.Debug.Log("[MCP] Server stopped");
        }

        private static void RunServerLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested && _listener?.IsListening == true)
            {
                try
                {
                    var context = _listener.GetContextAsync().Result;
                    Task.Run(() => HandleRequest(context), token);
                }
                catch (ObjectDisposedException)
                {
                    break; // Listener was closed
                }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested)
                    {
                        UnityEngine.Debug.LogError($"[MCP] Server loop error: {ex.Message}");
                    }
                }
            }
        }

        private static async Task HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                // CORS
                if (_settings?.enableCors == true)
                {
                    response.AddHeader("Access-Control-Allow-Origin", "*");
                    response.AddHeader("Access-Control-Allow-Methods", "POST, OPTIONS");
                    response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Authorization");
                }

                // Handle preflight
                if (request.HttpMethod == "OPTIONS")
                {
                    response.StatusCode = 200;
                    response.Close();
                    return;
                }

                // Auth check
                if (_settings?.requireAuth == true)
                {
                    var authHeader = request.Headers["Authorization"];
                    if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                    {
                        SendResponse(response, 401, McpProtocol.SerializeResponse(
                            McpProtocol.Response.Fail(null, -32600, "Unauthorized")));
                        return;
                    }

                    var token = authHeader.Substring("Bearer ".Length).Trim();
                    if (token != _settings.authToken)
                    {
                        SendResponse(response, 401, McpProtocol.SerializeResponse(
                            McpProtocol.Response.Fail(null, -32600, "Invalid token")));
                        return;
                    }
                }

                // Only accept POST to /mcp
                if (request.HttpMethod != "POST" || request.Url.AbsolutePath != "/mcp")
                {
                    SendResponse(response, 404, "Not Found");
                    return;
                }

                // Read request body
                string requestBody;
                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8))
                {
                    requestBody = await reader.ReadToEndAsync();
                }

                if (string.IsNullOrEmpty(requestBody))
                {
                    SendResponse(response, 400, McpProtocol.SerializeResponse(
                        McpProtocol.Response.InvalidRequest(null)));
                    return;
                }

                // Log request
                if (_settings?.logRequests == true)
                {
                    UnityEngine.Debug.Log($"[MCP] Request: {requestBody}");
                }

                // Process JSON-RPC request
                string responseJson = ProcessRequest(requestBody);

                // Log response
                if (_settings?.logResponses == true)
                {
                    UnityEngine.Debug.Log($"[MCP] Response: {responseJson}");
                }

                SendResponse(response, 200, responseJson);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[MCP] Request handling error: {ex.Message}");
                try
                {
                    SendResponse(response, 500, McpProtocol.SerializeResponse(
                        McpProtocol.Response.InternalError(null, ex.Message)));
                }
                catch { }
            }
        }

        private static string ProcessRequest(string json)
        {
            try
            {
                var request = McpProtocol.DeserializeRequest(json);

                // Handle batch requests
                if (request == null)
                {
                    return McpProtocol.SerializeResponse(McpProtocol.Response.ParseError(null));
                }

                // Single request
                return ProcessSingleRequest(request);
            }
            catch (JsonException)
            {
                return McpProtocol.SerializeResponse(McpProtocol.Response.ParseError(null));
            }
            catch (Exception ex)
            {
                return McpProtocol.SerializeResponse(McpProtocol.Response.InternalError(null, ex.Message));
            }
        }

        private static string ProcessSingleRequest(McpProtocol.Request request)
        {
            var id = request.Id;

            switch (request.Method)
            {
                case "initialize":
                    return HandleInitialize(id, request.Params);

                case "tools/list":
                    return HandleToolsList(id);

                case "tools/call":
                    return HandleToolCall(id, request.Params);

                case "ping":
                    return McpProtocol.SerializeResponse(McpProtocol.Response.Success(id, new { status = "ok" }));

                default:
                    return McpProtocol.SerializeResponse(McpProtocol.Response.MethodNotFound(id));
            }
        }

        private static string HandleInitialize(JToken id, JToken paramsElement)
        {
            var result = new McpProtocol.InitializeResult
            {
                ProtocolVersion = "2024-11-05",
                Capabilities = new McpProtocol.ServerCapabilities
                {
                    Tools = new McpProtocol.ToolsCapability { ListChanged = true }
                },
                ServerInfo = new McpProtocol.ServerInfo
                {
                    Name = "CueStrike MCP Server",
                    Version = "1.0.0"
                }
            };
            return McpProtocol.SerializeResponse(McpProtocol.Response.Success(id, result));
        }

        private static string HandleToolsList(JToken id)
        {
            var tools = _tools.Values.Select(t => new McpProtocol.Tool
            {
                Name = t.Name,
                Description = t.Description,
                InputSchema = t.InputSchema
            }).ToArray();

            return McpProtocol.SerializeResponse(McpProtocol.Response.Success(id, new { tools }));
        }

        private static string HandleToolCall(JToken id, JToken paramsElement)
        {
            try
            {
                var toolCall = JsonConvert.DeserializeObject<McpProtocol.ToolCallParams>(
                    paramsElement.ToString(),
                    new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() }
                );

                if (toolCall == null || string.IsNullOrEmpty(toolCall.Name))
                {
                    return McpProtocol.SerializeResponse(McpProtocol.Response.InvalidParams(id, "Missing tool name"));
                }

                if (!_tools.TryGetValue(toolCall.Name, out var tool))
                {
                    return McpProtocol.SerializeResponse(McpProtocol.Response.MethodNotFound(id));
                }

                var result = tool.Execute(toolCall.Arguments);
                return McpProtocol.SerializeResponse(McpProtocol.Response.Success(id, result));
            }
            catch (Exception ex)
            {
                return McpProtocol.SerializeResponse(McpProtocol.Response.InternalError(id, ex.Message));
            }
        }

        private static void SendResponse(HttpListenerResponse response, int statusCode, string content)
        {
            response.StatusCode = statusCode;
            response.ContentType = "application/json";
            var bytes = Encoding.UTF8.GetBytes(content);
            response.ContentLength64 = bytes.Length;
            response.OutputStream.Write(bytes, 0, bytes.Length);
            response.Close();
        }

        private static void LoadSettings()
        {
            var guids = AssetDatabase.FindAssets("t:McpSettings");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _settings = AssetDatabase.LoadAssetAtPath<McpSettings>(path);
            }

            if (_settings == null)
            {
                _settings = ScriptableObject.CreateInstance<McpSettings>();
                _settings.port = 8080;
            }
        }

        /// <summary>
        /// Gets all registered tools.
        /// </summary>
        public static IReadOnlyDictionary<string, IMcpTool> GetTools() => _tools;

        /// <summary>
        /// Gets the current settings.
        /// </summary>
        public static McpSettings GetSettings() => _settings;
    }
}