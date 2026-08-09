using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace CueStrike.MCP.Tools
{
    /// <summary>
    /// Tool for searching file contents in the Unity project using regex.
    /// </summary>
    public class SearchFilesTool : IMcpTool
    {
        public string Name => "search_files";
        public string Description => "Search for a regex pattern across files in the Unity project.";

        public JToken InputSchema => CreateSchema();

        private static JToken CreateSchema()
        {
            var schema = new
            {
                type = "object",
                properties = new
                {
                    path = new { type = "string", description = "Directory path relative to project root (e.g., 'Assets/Scripts'). Empty for project root." },
                    regex = new { type = "string", description = "Regular expression pattern to search for (Rust regex syntax)" },
                    filePattern = new { type = "string", description = "Glob pattern to filter files (e.g., '*.cs', '*.md')" },
                    maxResults = new { type = "integer", description = "Maximum number of results to return (default: 50)", minimum = 1, maximum = 500 },
                    contextLines = new { type = "integer", description = "Number of context lines before and after each match (default: 3)", minimum = 0, maximum = 20 }
                },
                required = new[] { "regex" }
            };
            return JToken.FromObject(schema);
        }

        public McpProtocol.ToolCallResult Execute(JToken arguments)
        {
            try
            {
                string regexPattern = arguments["regex"]?.Value<string>();
                string relativePath = "";
                if (arguments["path"] is JValue pathProp && pathProp.Type == JTokenType.String)
                    relativePath = pathProp.Value<string>();

                string filePattern = "*";
                if (arguments["filePattern"] is JValue patProp && patProp.Type == JTokenType.String)
                    filePattern = patProp.Value<string>();

                int maxResults = 50;
                if (arguments["maxResults"] is JValue maxProp && maxProp.Type == JTokenType.Integer)
                    maxResults = Math.Min(500, Math.Max(1, maxProp.Value<int>()));

                int contextLines = 3;
                if (arguments["contextLines"] is JValue ctxProp && ctxProp.Type == JTokenType.Integer)
                    contextLines = Math.Min(20, Math.Max(0, ctxProp.Value<int>()));

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

                var regex = new Regex(regexPattern, RegexOptions.Multiline);
                var results = new List<object>();
                int totalMatches = 0;

                var searchOption = SearchOption.AllDirectories;
                var files = Directory.GetFiles(fullPath, filePattern, searchOption);

                foreach (var file in files)
                {
                    if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                        continue;

                    try
                    {
                        string content = File.ReadAllText(file);
                        var lines = content.Split('\n');
                        var matches = regex.Matches(content);

                        foreach (Match match in matches)
                        {
                            if (totalMatches >= maxResults)
                                break;

                            int lineIndex = content.Substring(0, match.Index).Split('\n').Length - 1;
                            int startLine = Math.Max(0, lineIndex - contextLines);
                            int endLine = Math.Min(lines.Length - 1, lineIndex + contextLines);

                            var context = new List<object>();
                            for (int i = startLine; i <= endLine; i++)
                            {
                                context.Add(new
                                {
                                    line = i + 1,
                                    content = lines[i],
                                    isMatch = (i == lineIndex)
                                });
                            }

                            var relFile = GetRelativePath(projectRoot, file);
                            results.Add(new
                            {
                                file = relFile,
                                line = lineIndex + 1,
                                match = match.Value,
                                context = context
                            });

                            totalMatches++;
                        }
                    }
                    catch
                    {
                        // Skip files that can't be read
                    }

                    if (totalMatches >= maxResults)
                        break;
                }

                var result = new
                {
                    regex = regexPattern,
                    path = relativePath,
                    filePattern = filePattern,
                    totalMatches = totalMatches,
                    results = results
                };

                return McpProtocol.CreateJsonResult(result);
            }
            catch (Exception ex)
            {
                return McpProtocol.CreateTextResult($"Error searching files: {ex.Message}", true);
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