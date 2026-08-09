# MCP Unity Tools – Task List

## High‑Priority Tasks
- [x] MCP Server already implemented in `Assets/CueStrike/Editor/MCP/` (custom implementation)
- [x] Editor Window: `Tools → CueStrike → MCP Server` (McpServerWindow.cs)
- [x] Start MCP Server via Tools → CueStrike → MCP Server → ▶ Start Server
- [x] 5 Built-in tools registered: execute_code, read_file, write_file, list_files, search_files
- [ ] Install MCP Python client (`pip install mcp-cli`) — optional for external/Cline use
- [ ] Create MCP client config file (`%USERPROFILE%\.mcp\config.json`) with matching serverUrl and authToken

## Medium‑Priority Tasks
- [ ] **MCPSelfTest.cs** NOT yet created (planned MenuItem `Tools/CueStrike/MCP/Self-Test`) — file does not exist. Use `McpTestClient.cs` → Run All Tests. Planned suite included:
  - MCP Server running check
  - Tools registered check
  - Execute Code tool test
  - Read File tool test
  - List Files tool test
  - Search Files tool test
  - **Zero Pink Policy** check
  - **Audio Links** check
- [x] Existing **McpTestClient.cs** provides individual tool tests + Run All Tests
  - `Tools → CueStrike → MCP → Test Connection`
  - `Tools → CueStrike → MCP → Test Tools List`
  - `Tools → CueStrike → MCP → Test Execute Code`
  - `Tools → CueStrike → MCP → Test Read File`
  - `Tools → CueStrike → MCP → Test List Files`
  - `Tools → CueStrike → MCP → Test Search Files`
  - `Tools → CueStrike → MCP → Run All Tests`
- [ ] Add documentation **SetupReport_mcp_unity.md** (after self-test passes)
- [ ] Reference SetupReport in master doc

## Low‑Priority Tasks
- [ ] Extend MCP tools to support Create/Update/Delete of GameObjects, Components, and Scene assets
- [ ] Add unit-tests for MCP Editor scripts
- [ ] Update CI pipeline to run MCP self-test in headless mode
- [ ] Document advanced usage examples (e.g., batch instantiate prefabs, modify transforms, invoke methods)

## Notes
- All editor scripts include 3-layer guards (Play-mode block, Unsaved-changes prompt, Wrong-scene prompt) consistent with existing tools.
- Scripts placed under `Assets/CueStrike/Editor/` so they compile only in the editor.
- After each step, run the corresponding Unity MenuItem and verify console output for success/failure.
- **Zero Pink Policy**: No Standard shaders, no missing shaders (pink materials)
- **Audio Links**: Verify required audio clips exist in project