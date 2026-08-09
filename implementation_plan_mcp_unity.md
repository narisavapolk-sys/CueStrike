# MCP Unity Tools — Implementation Plan

## 1. Objectives
- ให้ AI (Cline) สามารถสื่อสารกับ Unity Editor ผ่าน MCP (Model Context Protocol) โดยตรง
- ลดขั้นตอนการเปิด/ปิด Unity เพื่อแก้ไข Scene, GameObject, Component
- สร้าง workflow ที่ปลอดภัย (guard, undo) สำหรับ AI

## 2. Prerequisites
1. **Python 3.10+** ติดตั้งอยู่บนเครื่อง (ตรวจสอบด้วย `python --version`)
2. **Unity 6000.4.4f1** (URP 17.4) มี **Universal Render Pipeline** แพคเกจติดตั้งแล้ว
3. **Git** สำหรับ version control

## 3. Architecture — Custom Implementation (✅ DONE)
ไม่ได้ใช้ external package `mcp-unity` — โปรเจกต์นี้มี **Custom MCP Server** แล้วใน `Assets/CueStrike/Editor/MCP/`

### Registered Tools (IMcpTool)
| Tool | Name | Description |
|------|------|-------------|
| ExecuteCodeTool | `execute_code` | Execute arbitrary C# code in Unity Editor |
| ReadFileTool | `read_file` | Read text file from project |
| WriteFileTool | `write_file` | Write text file to project |
| ListFilesTool | `list_files` | List files with pattern matching |
| SearchFilesTool | `search_files` | Regex search across files |

## 4. Installation Steps — ALREADY COMPLETE ✅

### 4.1. MCP Server is already in the project
Location: `Assets/CueStrike/Editor/MCP/`
- No git clone needed
- No manifest.json modification needed
- Scripts compile as Editor-only (under `Assets/CueStrike/Editor/`)

### 4.2. Start MCP Server
1. เปิด Unity
2. ไปที่ **Tools → CueStrike → MCP Server** (MenuItem จาก `McpServerWindow`)
3. ตั้งค่า Port (default 8080) และ Auth Token (optional)
4. กด **▶ Start Server**
5. ดู Console: `MCP Server started on http://localhost:8080/`

### 4.3. Test Connection
ใช้เมนูใน Unity:
- **Tools → CueStrike → MCP → Test Connection** — Ping server
- **Tools → CueStrike → MCP → Test Tools List** — List registered tools
- **Tools → CueStrike → MCP → Test Execute Code** — Run code in Editor
- **Tools → CueStrike → MCP → Test Read File** — Read file via MCP
- **Tools → CueStrike → MCP → Test List Files** — List directory
- **Tools → CueStrike → MCP → Test Search Files** — Regex search
- **Tools → CueStrike → MCP → Run All Tests** — Full test suite
- **Tools → CueStrike → MCP → Self-Test** — **NEW** Comprehensive self-test including Zero Pink Policy & Audio Links

### 4.4. Python Client (Optional - for Cline/external use)
```bash
python -m pip install --upgrade pip
python -m pip install mcp-cli
```
สร้าง config `%USERPROFILE%\.mcp\config.json`:
```json
{
  "serverUrl": "http://127.0.0.1:8080",
  "authToken": ""  // เหมือนใน McpSettings
}
```

## 5. Editor Scripts Created

### 5.1. MCPSelfTest.cs (PLANNED — NOT YET CREATED)
**File:** `Assets/CueStrike/Editor/MCPSelfTest.cs` ⚠️ **does not exist** — current testing uses `McpTestClient.cs` (`Tools/CueStrike/MCP/Run All Tests`)  
**Menu:** `Tools/CueStrike/MCP/Self-Test`  
**Tests:**
1. MCP Server running
2. Tools registered (5 tools)
3. MCP Settings asset exists
4. Execute Code tool works
5. Read File tool works
6. List Files tool works
7. Search Files tool works
8. **Zero Pink Policy** — No Standard/Hidden/Error shaders
9. **Audio Links** — Required audio clips exist

### 5.2. Existing Test Client (McpTestClient.cs)
**File:** `Assets/CueStrike/Editor/MCP/McpTestClient.cs`  
**Menu:** `Tools/CueStrike/MCP/Test *`  
Individual tool tests + Run All Tests

## 6. Setup Report
ไฟล์: `CueStrike_Project/SetupReport_mcp_unity.md` (จะสร้างหลัง self-test ผ่าน)

## 7. อัปเดต CUESTRIKE_MASTER.md
เพิ่ม Section **MCP Unity Tools** หลังหัวข้อ **10. DEV AGENT'S 7 RULES** — documenting actual implementation

## 8. สรุปขั้นตอนต่อไป
- [x] MCP Server implementation (custom, already done)
- [x] Editor Window (McpServerWindow) — ✅
- [x] Test Client (McpTestClient) — ✅
- [x] 5 Built-in Tools — ✅
- [ ] MCPSelfTest.cs with Zero Pink & Audio checks — ⏳ NOT YET CREATED (planned, file absent)
- [ ] Run Self-Test in Unity and verify
- [ ] Create SetupReport_mcp_unity.md
- [ ] Update CUESTRIKE_MASTER.md with MCP section
- [ ] Commit changes