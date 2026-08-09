using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEditor;
using UnityEngine;

namespace CueStrike.MCP.Tools
{
    /// <summary>
    /// Tool for listing files and directories in the Unity project.
    /// </summary>
    public class ListFilesTool : IMcpTool
    {
        public string Name => "list_files";
        public string Description => "List files and directories in the Unity project. Path is relative to project root.";

        public JToken InputSchema => CreateSchema();

        private static JToken CreateSchema()
        {
            var schema = new
            {
                type = "object",
                properties = new
                {
                    path = new { type = "string", description = "Directory path relative to project root (e.g., 'Assets/Scripts'). Empty for project root." },
                    recursive = new { type = "boolean", description = "Whether to list recursively (default: false)" },
                    pattern = new { type = "string", description = "Glob pattern to filter files (e.g., '*.cs', '*.prefab')" },
                    includeMeta = new { type = "boolean", description = "Include .meta files (default: false)" }
                }
            };
            return JToken.FromObject(schema);
        }

        public McpProtocol.ToolCallResult Execute(JToken arguments)
        {
            try
            {
                string relativePath = "";
                if (arguments["path"] is JValue pathProp && pathProp.Type == JTokenType.String)
                    relativePath = pathProp.Value<string>();

                bool recursive = arguments["recursive"]?.Value<bool>() ?? false;

                string pattern = "*";
                if (arguments["pattern"] is JValue patProp && patProp.Type == JTokenType.String)
                    pattern = patProp.Value<string>();

                bool includeMeta = arguments["includeMeta"]?.Value<bool>() ?? false;

                // Resolve path relative to project root
                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                string fullPath = Path.GetFullPath(Path.Combine(projectRoot, relativePath));

                // Security: ensure path is within project
                if (!fullPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
                {
                    return McpProtocol.CreateTextResult($"Error: Path '{relativePath}' is outside project directory", true);
                }

                if (!Directory.Exists(fullPath))
                {
                    return McpProtocol.CreateTextResult($"Error: Directory not found: {relativePath}", true);
                }

                var entries = new List<object>();

                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var files = Directory.GetFiles(fullPath, pattern, searchOption);
                var dirs = Directory.GetDirectories(fullPath, "*", searchOption);

                foreach (var dir in dirs)
                {
                    var relDir = GetRelativePath(projectRoot, dir);
                    entries.Add(new
                    {
                        type = "directory",
                        path = relDir,
                        name = Path.GetFileName(dir)
                    });
                }

                foreach (var file in files)
                {
                    if (!includeMeta && file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var relFile = GetRelativePath(projectRoot, file);
                    var info = new FileInfo(file);
                    entries.Add(new
                    {
                        type = "file",
                        path = relFile,
                        name = Path.GetFileName(file),
                        size = info.Length,
                        modified = info.LastWriteTimeUtc.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    });
                }

                var result = new
                {
                    path = relativePath,
                    count = entries.Count,
                    entries = entries
                };

                return McpProtocol.CreateJsonResult(result);
            }
            catch (Exception ex)
            {
                return McpProtocol.CreateTextResult($"Error listing files: {ex.Message}", true);
            }
        }

        private string GetRelativePath(string basePath, string fullPath)
        {
            var uri1 = new Uri(basePath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);
            var uri2 = new Uri(fullPath);
            return Uri.UnescapeDataString(uri1.MakeRelativeUri(uri2).ToString())
                .Replace('/', Path.DirectorySeparatorChar);
        }
    }
}