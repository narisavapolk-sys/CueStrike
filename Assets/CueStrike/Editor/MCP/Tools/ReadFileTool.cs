using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

using UnityEditor;
using UnityEngine;

namespace CueStrike.MCP.Tools
{
    /// <summary>
    /// Tool for reading files from the Unity project.
    /// </summary>
    public class ReadFileTool : IMcpTool
    {
        public string Name => "read_file";
        public string Description => "Read the contents of a file in the Unity project. Path is relative to project root.";

        public JToken InputSchema => CreateSchema();

        private static JToken CreateSchema()
        {
            var schema = new
            {
                type = "object",
                properties = new
                {
                    path = new { type = "string", description = "Path to the file relative to project root (e.g., 'Assets/Scripts/MyScript.cs')" },
                    startLine = new { type = "integer", description = "Starting line number (1-based, optional)", minimum = 1 },
                    endLine = new { type = "integer", description = "Ending line number (inclusive, optional)", minimum = 1 }
                },
                required = new[] { "path" }
            };
            return JToken.FromObject(schema);
        }

        public McpProtocol.ToolCallResult Execute(JToken arguments)
        {
            try
            {
                string relativePath = arguments["path"]?.Value<string>();
                int startLine = 1;
                int endLine = -1; // -1 means to end

                if (arguments["startLine"] is JValue startProp && startProp.Type == JTokenType.Integer)
                    startLine = startProp.Value<int>();

                if (arguments["endLine"] is JValue endProp && endProp.Type == JTokenType.Integer)
                    endLine = endProp.Value<int>();

                // Resolve path relative to project root
                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                string fullPath = Path.GetFullPath(Path.Combine(projectRoot, relativePath));

                // Security: ensure path is within project
                if (!fullPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
                {
                    return McpProtocol.CreateTextResult($"Error: Path '{relativePath}' is outside project directory", true);
                }

                if (!File.Exists(fullPath))
                {
                    return McpProtocol.CreateTextResult($"Error: File not found: {relativePath}", true);
                }

                string content = File.ReadAllText(fullPath);

                // Apply line range if specified
                if (startLine > 1 || endLine > 0)
                {
                    var lines = content.Split('\n');
                    int startIdx = Math.Max(0, startLine - 1);
                    int endIdx = endLine > 0 ? Math.Min(lines.Length - 1, endLine - 1) : lines.Length - 1;

                    if (startIdx >= lines.Length)
                    {
                        return McpProtocol.CreateTextResult($"Error: startLine {startLine} exceeds file length ({lines.Length} lines)", true);
                    }

                    content = string.Join("\n", lines, startIdx, endIdx - startIdx + 1);
                }

                var result = new
                {
                    path = relativePath,
                    content = content,
                    lines = content.Split('\n').Length,
                    size = new FileInfo(fullPath).Length
                };

                return McpProtocol.CreateJsonResult(result);
            }
            catch (Exception ex)
            {
                return McpProtocol.CreateTextResult($"Error reading file: {ex.Message}", true);
            }
        }
    }
}