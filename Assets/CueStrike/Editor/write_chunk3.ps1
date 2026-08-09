$content = @"
        static async Task<bool> TestExecuteCodeTool()
        {
            try
            {
                if (!CueStrike.MCP.McpServer.IsRunning) return false;

                var code = @\"var go = new UnityEngine.GameObject(\"\"MCP_Test_Object\"\"); UnityEngine.Debug.Log(\"\"Created test object: \"\" + go.name); UnityEngine.Object.DestroyImmediate(go); return new { success = true, objectName = go.name };\";

                var args = new { code = code };
                var result = await CueStrike.MCP.McpTestClient.SendRequest(\"tools/call\", new { name = \"execute_code\", arguments = args });
                return result != null;
            }
            catch
            {
                return false;
            }
        }

        static async Task<bool> TestReadFileTool()
        {
            try
            {
                if (!CueStrike.MCP.McpServer.IsRunning) return false;

                var readArgs = new { path = \"Assets/CueStrike/Editor/MCP/McpServer.cs\" };
                var result = await CueStrike.MCP.McpTestClient.SendRequest(\"tools/call\", new { name = \"read_file\", arguments = readArgs });
                return result != null;
            }
            catch
            {
                return false;
            }
        }

        static async Task<bool> TestListFilesTool()
        {
            try
            {
                if (!CueStrike.MCP.McpServer.IsRunning) return false;

                var listArgs = new { path = \"Assets/CueStrike/Editor/MCP\", pattern = \"*.cs\" };
                var result = await CueStrike.MCP.McpTestClient.SendRequest(\"tools/call\", new { name = \"list_files\", arguments = listArgs });
                return result != null;
            }
            catch
            {
                return false;
            }
        }

        static async Task<bool> TestSearchFilesTool()
        {
            try
            {
                if (!CueStrike.MCP.McpServer.IsRunning) return false;

                var searchArgs = new { regex = \"public static\", path = \"Assets/CueStrike/Editor/MCP\", filePattern = \"*.cs\" };
                var result = await CueStrike.MCP.McpTestClient.SendRequest(\"tools/call\", new { name = \"search_files\", arguments = searchArgs });
                return result != null;
            }
            catch
            {
                return false;
            }
        }
"@
$content | Add-Content 'c:\Users\mongo\UnityProjects\CueStrike\CueStrike_Project\Assets\CueStrike\Editor\MCPSelfTest.cs' -Encoding UTF8