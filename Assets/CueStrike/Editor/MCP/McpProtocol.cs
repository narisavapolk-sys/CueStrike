using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;



namespace CueStrike.MCP
{
    /// <summary>
    /// JSON-RPC 2.0 protocol implementation for MCP communication.
    /// </summary>
    public static class McpProtocol
    {
        public const string JsonRpcVersion = "2.0";

        /// <summary>
        /// Represents a JSON-RPC 2.0 Request.
        /// </summary>
        public class Request
        {
            [JsonProperty("jsonrpc")]
            public string JsonRpc { get; set; } = JsonRpcVersion;

            [JsonProperty("id")]
            public JToken Id { get; set; }

            [JsonProperty("method")]
            public string Method { get; set; }

            [JsonProperty("params")]
            public JToken Params { get; set; }

            public bool IsNotification => Id == null || Id.Type == JTokenType.Undefined || Id.Type == JTokenType.Null;
        }

        /// <summary>
        /// Represents a JSON-RPC 2.0 Response.
        /// </summary>
        public class Response
        {
            [JsonProperty("jsonrpc")]
            public string JsonRpc { get; set; } = JsonRpcVersion;

            [JsonProperty("id")]
            public JToken Id { get; set; }

            [JsonProperty("result")]
            public object Result { get; set; }

            [JsonProperty("error")]
            public Error Error { get; set; }

            public bool IsSuccess => Error == null;

            public static Response Success(JToken id, object result)
            {
                return new Response { Id = id, Result = result };
            }

            public static Response Fail(JToken id, int code, string message, object data = null)
            {
                return new Response { Id = id, Error = new Error { Code = code, Message = message, Data = data } };
            }

            public static Response ParseError(JToken id)
            {
                return Fail(id, -32700, "Parse error");
            }

            public static Response InvalidRequest(JToken id)
            {
                return Fail(id, -32600, "Invalid Request");
            }

            public static Response MethodNotFound(JToken id)
            {
                return Fail(id, -32601, "Method not found");
            }

            public static Response InvalidParams(JToken id, string message = "Invalid params")
            {
                return Fail(id, -32602, message);
            }

            public static Response InternalError(JToken id, string message = "Internal error")
            {
                return Fail(id, -32603, message);
            }
        }

        /// <summary>
        /// JSON-RPC Error object.
        /// </summary>
        public class Error
        {
            [JsonProperty("code")]
            public int Code { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("data")]
            public object Data { get; set; }
        }

        /// <summary>
        /// Tool definition for MCP capabilities.
        /// </summary>
        public class Tool
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("description")]
            public string Description { get; set; }

            [JsonProperty("inputSchema")]
            public JToken InputSchema { get; set; }
        }

        /// <summary>
        /// Tool call request parameters.
        /// </summary>
        public class ToolCallParams
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("arguments")]
            public JToken Arguments { get; set; }
        }

        /// <summary>
        /// Tool call result.
        /// </summary>
        public class ToolCallResult
        {
            [JsonProperty("content")]
            public List<Content> Content { get; set; } = new();

            [JsonProperty("isError")]
            public bool IsError { get; set; }
        }

        /// <summary>
        /// Content item for tool results.
        /// </summary>
        public class Content
        {
            [JsonProperty("type")]
            public string Type { get; set; } = "text";

            [JsonProperty("text")]
            public string Text { get; set; }
        }

        /// <summary>
        /// Initialize request params.
        /// </summary>
        public class InitializeParams
        {
            [JsonProperty("protocolVersion")]
            public string ProtocolVersion { get; set; }

            [JsonProperty("capabilities")]
            public JToken Capabilities { get; set; }

            [JsonProperty("clientInfo")]
            public ClientInfo ClientInfo { get; set; }
        }

        public class ClientInfo
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("version")]
            public string Version { get; set; }
        }

        /// <summary>
        /// Initialize response result.
        /// </summary>
        public class InitializeResult
        {
            [JsonProperty("protocolVersion")]
            public string ProtocolVersion { get; set; } = "2024-11-05";

            [JsonProperty("capabilities")]
            public ServerCapabilities Capabilities { get; set; } = new();

            [JsonProperty("serverInfo")]
            public ServerInfo ServerInfo { get; set; } = new();
        }

        public class ServerCapabilities
        {
            [JsonProperty("tools")]
            public ToolsCapability Tools { get; set; } = new();
        }

        public class ToolsCapability
        {
            [JsonProperty("listChanged")]
            public bool ListChanged { get; set; } = true;
        }

        public class ServerInfo
        {
            [JsonProperty("name")]
            public string Name { get; set; } = "CueStrike MCP Server";

            [JsonProperty("version")]
            public string Version { get; set; } = "1.0.0";
        }

        /// <summary>
        /// Serializes a response to JSON string.
        /// </summary>
        public static string SerializeResponse(Response response)
        {
            var options = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.None
            };
            return JsonConvert.SerializeObject(response, options);
        }

        /// <summary>
        /// Deserializes a request from JSON string.
        /// </summary>
        public static Request DeserializeRequest(string json)
        {
            var options = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            return JsonConvert.DeserializeObject<Request>(json, options);
        }

        /// <summary>
        /// Creates a tool result with text content.
        /// </summary>
        public static ToolCallResult CreateTextResult(string text, bool isError = false)
        {
            return new ToolCallResult
            {
                Content = new List<Content> { new Content { Type = "text", Text = text } },
                IsError = isError
            };
        }

        /// <summary>
        /// Creates a tool result with JSON content (serialized to text).
        /// </summary>
        public static ToolCallResult CreateJsonResult(object obj, bool isError = false)
        {
            var options = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.Indented
            };
            return new ToolCallResult
            {
                Content = new List<Content> { new Content { Type = "text", Text = JsonConvert.SerializeObject(obj, options) } },
                IsError = isError
            };
        }
    }
}