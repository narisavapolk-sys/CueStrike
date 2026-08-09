// 🧪 McpProtocolTests — verifies the JSON-RPC 2.0 "MCP pipe" still works 100%.
// Rule: Test First — proves the MCP transport (Newtonsoft) round-trips correctly.
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using CueStrike.MCP;

namespace CueStrike.Tests.Editor
{
    public class McpProtocolTests
    {
        [Test]
        public void JsonRpcVersion_Is20()
        {
            Assert.AreEqual("2.0", McpProtocol.JsonRpcVersion);
        }

        [Test]
        public void Request_DeserializesMethodAndId()
        {
            var req = McpProtocol.DeserializeRequest(
                "{\"jsonrpc\":\"2.0\",\"id\":7,\"method\":\"tools/list\",\"params\":{}}");

            Assert.AreEqual("tools/list", req.Method);
            Assert.AreEqual(7, (int)req.Id);
            Assert.IsFalse(req.IsNotification);
        }

        [Test]
        public void Request_WithoutId_IsNotification()
        {
            var req = McpProtocol.DeserializeRequest(
                "{\"jsonrpc\":\"2.0\",\"method\":\"log/notify\"}");
            Assert.IsTrue(req.IsNotification);
        }

        [Test]
        public void Response_Success_SerializesCamelCaseJson()
        {
            var resp = McpProtocol.Response.Success(1, new { ok = true });
            string json = McpProtocol.SerializeResponse(resp);

            Assert.IsTrue(json.Contains("\"jsonrpc\":\"2.0\""));
            Assert.IsTrue(json.Contains("\"result\""));
            Assert.IsTrue(json.Contains("\"ok\":true"));
            Assert.IsFalse(json.Contains("\"error\""));
        }

        [Test]
        public void Response_Fail_HasErrorAndNotSuccess()
        {
            var resp = McpProtocol.Response.MethodNotFound(2);

            Assert.IsFalse(resp.IsSuccess);
            Assert.IsNotNull(resp.Error);
            Assert.AreEqual(-32601, resp.Error.Code);
            Assert.AreEqual("Method not found", resp.Error.Message);
        }

        [Test]
        public void CreateTextResult_ProducesTextContent()
        {
            var r = McpProtocol.CreateTextResult("hello world");
            Assert.IsFalse(r.IsError);
            Assert.AreEqual("text", r.Content[0].Type);
            Assert.AreEqual("hello world", r.Content[0].Text);
        }

        [Test]
        public void CreateJsonResult_SerializesAndRoundTrips()
        {
            var payload = new McpProtocol.InitializeResult();
            payload.ProtocolVersion = "2024-11-05";

            var r = McpProtocol.CreateJsonResult(payload);
            var parsed = JObject.Parse(r.Content[0].Text);

            Assert.AreEqual("2024-11-05", (string)parsed["protocolVersion"]);
            Assert.IsNotNull(parsed["capabilities"]);
        }
    }
}
