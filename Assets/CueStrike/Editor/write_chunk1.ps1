$content = @"
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace CueStrike.Editor
{
    public static class MCPSelfTest
    {
        private const string MENU_PATH = \"Tools/CueStrike/MCP/Self-Test\";

        [MenuItem(MENU_PATH, false, 100)]
        public static void RunSelfTest()
        {
            var results = new List<string>();
            bool allPassed = true;

            results.Add(\"=== MCP Unity Tools Self-Test ===\");
            results.Add(\"\");

            bool serverRunning = CueStrike.MCP.McpServer.IsRunning;
            results.Add(serverRunning ? \"PASS: MCP Server is running\" : \"FAIL: MCP Server is NOT running (open Tools CueStrike MCP Server and click Start)\");
            if (!serverRunning) allPassed = false;

            var tools = CueStrike.MCP.McpServer.GetTools();
            int toolCount = tools.Count;
            results.Add(toolCount > 0 ? `$"PASS: {toolCount} MCP tools registered"` : \"FAIL: No MCP tools registered\");
            if (toolCount == 0) allPassed = false;

            foreach (var tool in tools)
            {
                results.Add(`$"   {tool.Key}: {tool.Value.Description}"`);
            }

            bool settingsExist = CheckMCPSettings();
            results.Add(settingsExist ? \"PASS: MCP Settings asset found\" : \"WARN: MCP Settings asset not found\");
"@
$content | Set-Content 'c:\Users\mongo\UnityProjects\CueStrike\CueStrike_Project\Assets\CueStrike\Editor\MCPSelfTest.cs' -Encoding UTF8