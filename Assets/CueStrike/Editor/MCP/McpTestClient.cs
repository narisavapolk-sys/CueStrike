using System.Linq;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace CueStrike.MCP
{
    /// <summary>
    /// Test client for the MCP Server. Run this from the Unity Editor to verify the server works.
    /// </summary>
    public static class McpTestClient
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        [MenuItem("CueStrike/MCP/Test Connection", false, 101)]
        public static async void TestConnection()
        {
            if (!McpServer.IsRunning)
            {
                EditorUtility.DisplayDialog("MCP Test", "Server is not running. Start it from CueStrike > MCP Server window.", "OK");
                return;
            }

            try
            {
                var result = await SendRequest("ping", null);
                EditorUtility.DisplayDialog("MCP Test", $"Connection successful!\nResponse: {result}", "OK");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("MCP Test", $"Connection failed: {ex.Message}", "OK");
            }
        }

        [MenuItem("CueStrike/MCP/Test Tools List", false, 102)]
        public static async void TestToolsList()
        {
            if (!McpServer.IsRunning)
            {
                EditorUtility.DisplayDialog("MCP Test", "Server is not running.", "OK");
                return;
            }

            try
            {
                var result = await SendRequest("tools/list", null);
                var json = JsonConvert.SerializeObject(result, new JsonSerializerSettings { Formatting = Formatting.Indented });
                EditorUtility.DisplayDialog("MCP Test - Tools List", json.Length > 1000 ? json.Substring(0, 1000) + "..." : json, "OK");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("MCP Test", $"Failed: {ex.Message}", "OK");
            }
        }

        [MenuItem("CueStrike/MCP/Test Execute Code", false, 103)]
        public static async void TestExecuteCode()
        {
            if (!McpServer.IsRunning)
            {
                EditorUtility.DisplayDialog("MCP Test", "Server is not running.", "OK");
                return;
            }

            try
            {
                var code = @"
                    var go = new UnityEngine.GameObject(""MCP_Test_Object"");
                    UnityEngine.Debug.Log(""Created test object: "" + go.name);
                    return new { success = true, objectName = go.name };
                ";

                var args = new { code = code };
                var result = await SendRequest("tools/call", new { name = "execute_code", arguments = args });
                var json = JsonConvert.SerializeObject(result, new JsonSerializerSettings { Formatting = Formatting.Indented });
                EditorUtility.DisplayDialog("MCP Test - Execute Code", json.Length > 1000 ? json.Substring(0, 1000) + "..." : json, "OK");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("MCP Test", $"Failed: {ex.Message}", "OK");
            }
        }

        [MenuItem("CueStrike/MCP/Test Read File", false, 104)]
        public static async void TestReadFile()
        {
            if (!McpServer.IsRunning)
            {
                EditorUtility.DisplayDialog("MCP Test", "Server is not running.", "OK");
                return;
            }

            try
            {
                var args = new { path = "Assets/CueStrike/Editor/MCP/McpServer.cs" };
                var result = await SendRequest("tools/call", new { name = "read_file", arguments = args });
                var json = JsonConvert.SerializeObject(result, new JsonSerializerSettings { Formatting = Formatting.Indented });
                EditorUtility.DisplayDialog("MCP Test - Read File", json.Length > 1000 ? json.Substring(0, 1000) + "..." : json, "OK");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("MCP Test", $"Failed: {ex.Message}", "OK");
            }
        }

        [MenuItem("CueStrike/MCP/Test List Files", false, 105)]
        public static async void TestListFiles()
        {
            if (!McpServer.IsRunning)
            {
                EditorUtility.DisplayDialog("MCP Test", "Server is not running.", "OK");
                return;
            }

            try
            {
                var args = new { path = "Assets/CueStrike/Editor/MCP", recursive = true, pattern = "*.cs" };
                var result = await SendRequest("tools/call", new { name = "list_files", arguments = args });
                var json = JsonConvert.SerializeObject(result, new JsonSerializerSettings { Formatting = Formatting.Indented });
                EditorUtility.DisplayDialog("MCP Test - List Files", json.Length > 1000 ? json.Substring(0, 1000) + "..." : json, "OK");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("MCP Test", $"Failed: {ex.Message}", "OK");
            }
        }

        [MenuItem("CueStrike/MCP/Test Search Files", false, 106)]
        public static async void TestSearchFiles()
        {
            if (!McpServer.IsRunning)
            {
                EditorUtility.DisplayDialog("MCP Test", "Server is not running.", "OK");
                return;
            }

            try
            {
                var args = new { regex = "McpServer", path = "Assets/CueStrike/Editor/MCP", filePattern = "*.cs" };
                var result = await SendRequest("tools/call", new { name = "search_files", arguments = args });
                var json = JsonConvert.SerializeObject(result, new JsonSerializerSettings { Formatting = Formatting.Indented });
                EditorUtility.DisplayDialog("MCP Test - Search Files", json.Length > 1000 ? json.Substring(0, 1000) + "..." : json, "OK");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("MCP Test", $"Failed: {ex.Message}", "OK");
            }
        }

        [MenuItem("CueStrike/MCP/Run All Tests", false, 107)]
        public static async void RunAllTests()
        {
            if (!McpServer.IsRunning)
            {
                EditorUtility.DisplayDialog("MCP Test", "Server is not running.", "OK");
                return;
            }

            var results = new List<string>();

            try
            {
                // Test 1: Ping
                var pingResult = await SendRequest("ping", null);
                results.Add($"✓ Ping: {JsonConvert.SerializeObject(pingResult)}");
            }
            catch (Exception ex) { results.Add($"✗ Ping: {ex.Message}"); }

            try
            {
                // Test 2: Tools list
                var toolsResult = await SendRequest("tools/list", null);
                var tools = toolsResult as JObject;
                int count = tools?["tools"]?.Children().Count() ?? 0;
                results.Add($"✓ Tools List: {count} tools");
            }
            catch (Exception ex) { results.Add($"✗ Tools List: {ex.Message}"); }

            try
            {
                // Test 3: Execute code
                var code = "return new { test = \"execute_code works!\", time = System.DateTime.Now };";
                var args = new { code = code };
                var execResult = await SendRequest("tools/call", new { name = "execute_code", arguments = args });
                results.Add($"✓ Execute Code: OK");
            }
            catch (Exception ex) { results.Add($"✗ Execute Code: {ex.Message}"); }

            try
            {
                // Test 4: Read file
                var readArgs = new { path = "Assets/CueStrike/Editor/MCP/McpServer.cs" };
                var readResult = await SendRequest("tools/call", new { name = "read_file", arguments = readArgs });
                results.Add($"✓ Read File: OK");
            }
            catch (Exception ex) { results.Add($"✗ Read File: {ex.Message}"); }

            try
            {
                // Test 5: List files
                var listArgs = new { path = "Assets/CueStrike/Editor/MCP", pattern = "*.cs" };
                var listResult = await SendRequest("tools/call", new { name = "list_files", arguments = listArgs });
                results.Add($"✓ List Files: OK");
            }
            catch (Exception ex) { results.Add($"✗ List Files: {ex.Message}"); }

            try
            {
                // Test 6: Search files
                var searchArgs = new { regex = "public static", path = "Assets/CueStrike/Editor/MCP", filePattern = "*.cs" };
                var searchResult = await SendRequest("tools/call", new { name = "search_files", arguments = searchArgs });
                results.Add($"✓ Search Files: OK");
            }
            catch (Exception ex) { results.Add($"✗ Search Files: {ex.Message}"); }

            EditorUtility.DisplayDialog("MCP Test - All Results", string.Join("\n", results), "OK");
        }

        private static async Task<object> SendRequest(string method, object @params)
        {
            var request = new
            {
                jsonrpc = "2.0",
                id = Guid.NewGuid().ToString(),
                method = method,
                @params = @params
            };

            var json = JsonConvert.SerializeObject(request, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"{McpServer.ServerUrl}mcp";

            var response = await _httpClient.PostAsync(url, content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"HTTP {response.StatusCode}: {responseJson}");
            }

            var responseObj = JObject.Parse(responseJson);

            if (responseObj["error"] != null)
            {
                throw new Exception($"MCP Error: {responseObj["error"]?["message"]?.Value<string>()}");
            }

            return responseObj["result"];
        }
    }
}