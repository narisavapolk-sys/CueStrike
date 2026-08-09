using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

using UnityEditor;
using UnityEngine;

namespace CueStrike.MCP.Tools
{
    /// <summary>
    /// Tool for writing files to the Unity project.
    /// </summary>
    public class WriteFileTool : IMcpTool
    {
        public string Name => "write_file";
        public string Description => "Write content to a file in the Unity project. Creates directories as needed. Path is relative to project root.";

        public JToken InputSchema => CreateSchema();

        private static JToken CreateSchema()
        {
            var schema = new
            {
                type = "object",
                properties = new
                {
                    path = new { type = "string", description = "Path to the file relative to project root (e.g., 'Assets/Scripts/MyScript.cs')" },
                    content = new { type = "string", description = "Content to write to the file" },
                    overwrite = new { type = "boolean", description = "Whether to overwrite if file exists (default: true)" }
                },
                required = new[] { "path", "content" }
            };
            return JToken.FromObject(schema);
        }

        public McpProtocol.ToolCallResult Execute(JToken arguments)
        {
            try
            {
                string relativePath = arguments["path"]?.Value<string>();
                string content = arguments["content"]?.Value<string>();
                bool overwrite = arguments["overwrite"]?.Value<bool>() ?? true;

                // Resolve path relative to project root
                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                string fullPath = Path.GetFullPath(Path.Combine(projectRoot, relativePath));

                // Security: ensure path is within project
                if (!fullPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
                {
                    return McpProtocol.CreateTextResult($"Error: Path '{relativePath}' is outside project directory", true);
                }

                // Check if file exists
                if (File.Exists(fullPath) && !overwrite)
                {
                    return McpProtocol.CreateTextResult($"Error: File already exists: {relativePath} (set overwrite: true to replace)", true);
                }

                // Create directory if needed
                string directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(fullPath, content);

                // Refresh AssetDatabase if it's in Assets folder
                if (relativePath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                {
                    AssetDatabase.Refresh();
                }

                var result = new
                {
                    path = relativePath,
                    success = true,
                    size = new FileInfo(fullPath).Length,
                    message = $"File written successfully ({new FileInfo(fullPath).Length} bytes)"
                };

                return McpProtocol.CreateJsonResult(result);
            }
            catch (Exception ex)
            {
                return McpProtocol.CreateTextResult($"Error writing file: {ex.Message}", true);
            }
        }
    }
}