using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace CueStrike.MCP.Tools
{
    /// <summary>
    /// Interface for all MCP tools.
    /// </summary>
    public interface IMcpTool
    {
        /// <summary>
        /// The name of the tool (used in MCP protocol).
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Human-readable description of what the tool does.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// JSON Schema for the tool's input parameters.
        /// </summary>
        JToken InputSchema { get; }

        /// <summary>
        /// Executes the tool with the given arguments.
        /// </summary>
        /// <param name="arguments">JSON arguments from the request</param>
        /// <returns>Tool call result</returns>
        McpProtocol.ToolCallResult Execute(JToken arguments);
    }
}