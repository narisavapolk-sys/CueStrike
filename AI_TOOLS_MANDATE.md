# 🛠️ CueStrike — AI TOOLS MANDATE (บังคับใช้กับ AI ทุกตัว)
> **Project:** CueStrike VR Billiards (AAA Unity, Meta Quest 2/3)
> **Version:** 1.0 | **Date:** 2026-08-06
> **อนุมัติโดย:** พี่โม่ง (Project Owner)
> **ผูกพัน:** AI ทุกตัวที่แตะโปรเจกต์นี้ (นาริ, Cline, Cursor, โค้ช, และ AI ตัวอื่นๆ ทั้งหมด)

---

## ⚖️ กฎเหล็ก 5 ข้อ (อ่านก่อนทำงานทุกครั้ง)

### ข้อ 1 — ใช้ TOOLS ก่อนเสมอ ห้ามเดา ห้ามมโน
- ก่อนรายงานว่าอะไร "เสร็จแล้ว / มีอยู่ / ใช้ได้" **ต้องตรวจไฟล์จริงด้วย tool** (read_file, list_files, search_files, run command) ก่อนทุกครั้ง
- ห้ามรายงานจากความจำหรือจากเอกสารเก่าโดยไม่ verify
- ถ้า tool ไม่มี ให้บอกพี่โม่งตรงๆ ว่า "ตรวจไม่ได้" — ห้ามแต่งคำตอบ

### ข้อ 2 — ทำงานเสร็จ = ต้องอัปเดตเอกสาร .md ในครั้งเดียวกัน
- **งานไม่ถือว่าเสร็จ** จนกว่าเอกสารที่เกี่ยวข้องจะถูกอัปเดต:
  - แก้โค้ด/สร้างไฟล์ → อัปเดต `CUESTRIKE_MASTER.md` (Phase Status) + `TASK_PROGRESS.md`
  - สร้างระบบใหม่ → เขียน/อัปเดตเอกสารแผนของระบบนั้น
  - ลบ/ย้ายไฟล์ → แก้เอกสารที่อ้างถึงไฟล์นั้นทันที
- ห้ามเขียนเอกสารอ้างไฟล์ที่ไม่มีจริง (เคยพบ: `MCPSelfTest.cs`, `CueStrikePinkMaterialFixer.cs`, `CueStrikeAIEasy/Medium/Hard.cs` ถูกอ้างในเอกสารแต่ไม่มีไฟล์จริง)
- ห้ามทิ้งไฟล์ซ้ำแล้วบอกว่า "cleanup complete" (เคยพบ: ไฟล์ .cs ซ้ำ 9 คู่)

### ข้อ 3 — รูปแบบการรายงาน: สั้น ชัด มีหลักฐาน
- รายงานด้วยตาราง/ bullet สั้นๆ ไม่เขียนยาว
- ทุก claim ต้องมีหลักฐาน: path ไฟล์, ผล compile, หรือ screenshot
- ถ้าพี่โม่งส่ง screenshot มา → วิเคราะห์จากภาพจริง (Vision) ห้ามเดาจากโค้ดล้วนๆ

### ข้อ 4 — ความปลอดภัยของโปรเจกต์
- AI แก้ไขไฟล์ `.unity` (Scene) **ได้โดยตรง** ผ่าน Editor/Batchmode Automation (`EditorSceneManager` + `-executeMethod`) — ไม่ต้องเขียนสคริปต์อ้อมให้พี่กด Apply อีก (Rule #4 Revolution)
- **Vision Audit บังคับ** — ทุกการแก้ Scene ต้องมาพร้อม screenshot ใหม่ส่งให้โม่งตรวจจากภาพจริงก่อนยอมรับ (เหลือ `Rule 3` + `Vision AI Workflow` p.95)
- URP เท่านั้น — ห้าม Standard Shader (สีชมพู)
- Normcore scripts ต้องมี `#if CUESTRIKE_NORMCORE` guard เสมอ
- ก่อนส่งงาน: compile ต้อง 0 errors (รัน batchmode หรือ Test Runner)
- ปิด Unity Editor ก่อนแก้โค้ดจากภายนอกเสมอ

### ข้อ 5 — ทำงานเป็นลำดับ ไม่มั่ว ไม่ข้าม
- ทำตาม Phase ใน `CUESTRIKE_MASTER.md` เท่านั้น
- งานใหญ่ต้องมี plan ก่อนลงมือ (implementation_plan + task list)
- หยุดทันทีเมื่อพี่โม่งสั่ง

### ข้อ 6 — Rule of Runtime Integrity (กฎความสมบูรณ์ของระบบรันไทม์)
- ห้ามแก้ปัญหาคอมไพล์ (Compile Error) ด้วยการฉีด DLL / Library ภายนอกที่ไม่ได้มาจาก Unity Package Manager (UPM) หรือมาตรฐาน Unity Editor Runtime โดยเด็ดขาด
- ใช้เครื่องมือมาตรฐานที่ Unity รองรับเสมอ (เช่น `JsonUtility`, `Newtonsoft.Json` ผ่าน UPM)
- คำนึงถึงการคอมไพล์ผ่าน IL2CPP และความเสถียรระยะยาว มากกว่าการทำให้ Error หายชั่วคราว
- หากพบ Technology Mismatch ต้องรายงานความจริงต่อโค้ช / พี่โม่งทันที ห้ามฝืนทำต่อ

---

## 🧰 TOOL STACK มาตรฐานของโปรเจกต์ (ต้องใช้ให้เป็น)

| # | Tool | ใช้ทำอะไร | สถานะ |
|---|------|-----------|-------|
| 1 | **MCP Unity Server** (custom ในโปรเจกต์) | AI คุยกับ Unity Editor โดยตรง: execute_code, read/write/list/search files | ✅ มีแล้ว (`Assets/CueStrike/Editor/MCP/`) |
| 2 | **Vision AI** (Claude 3.5 Sonnet / GPT-4o) | ดู screenshot จริง → แก้บัคจากภาพ ไม่หลอนรายงานทิพย์ | ⚙️ พี่โม่งตั้งค่า model ใน Cline Settings |
| 3 | **Unity Test Runner / Batchmode Compile** | ตรวจ compile 0 errors ก่อนส่งงานทุกครั้ง — **compile gate 2 ชั้น: (1) local `tools/compile_check.sh` + pre-commit hook (2) GitHub Actions `compile-gate.yml` ทุก PR (รอ secret `UNITY_LICENSE`)** | ✅ ใช้ได้ทันที |
| 4 | **Unity Muse** (Texture/Sprite/Sound) | เจน Texture/เสียงใน Editor โดยตรง | ⏳ รอพี่โม่งติดตั้งผ่าน Package Manager |
| 5 | **Stable Audio / ElevenLabs** | สร้างเสียง SFX + เสียงพากย์ลุงโน๊กจริง (~50 คลิปที่ขาด) | ⏳ รอ API Key จากพี่โม่ง |
| 6 | **Animation Rigging / VRIK** | Procedural animation ตัวละคร (ก้มแทง, หันมองลูก) | ⏳ รอติดตั้งผ่าน Package Manager |

---

## 🔌 MCP UNITY — แผนติดตั้งและใช้งาน

### สถานะปัจจุบัน
โปรเจกต์มี **Custom MCP Server อยู่แล้ว** ที่ `Assets/CueStrike/Editor/MCP/`:
- `McpServer.cs` — HTTP JSON-RPC 2.0 server (port 8080)
- `McpSettings.cs` — ScriptableObject config (port, auth token, CORS)
- `McpServerWindow.cs` — GUI: `Tools → CueStrike → MCP Server`
- `McpTestClient.cs` — เมนูทดสอบ `Tools → CueStrike → MCP → Test *`
- Tools 5 ตัว: `execute_code`, `read_file`, `write_file`, `list_files`, `search_files`

### วิธีเปิดใช้ (พี่โม่งทำครั้งเดียว)
1. เปิด Unity → `Tools → CueStrike → MCP Server`
2. กด **▶ Start Server** → Console ขึ้น `MCP Server started on http://localhost:8080/`
3. ทดสอบ: `Tools → CueStrike → MCP → Run All Tests`

### การเชื่อม Cline/AI ภายนอก
```json
// %USERPROFILE%\.mcp\config.json
{
  "serverUrl": "http://127.0.0.1:8080",
  "authToken": ""
}
```

### กฎการใช้ MCP
- AI ต้องใช้ MCP tools แทนการถามพี่โม่งว่า "ไฟล์นี้มีไหม" — ใช้ `list_files`/`search_files` เช็คเอง
- ใช้ `execute_code` สร้าง/แก้ GameObject ได้ แต่ **ห้ามบันทึก Scene ทับ** โดยไม่บอกพี่โม่ง
- ทุก action ผ่าน MCP ต้องรองรับ Undo (`Undo.RecordObject`)

---

## 👁️ VISION AI WORKFLOW (แก้ปัญหา AI หลอน)

เมื่อพี่โม่งส่ง screenshot บัค (สีชมพู / หน้าจอพัง / Safe Mode):
1. AI ต้องวิเคราะห์ **จากภาพจริงก่อน** แล้วค่อยไล่โค้ด
2. ระบุตำแหน่งปัญหาจากภาพ: object ไหน, material ไหน, shader อะไร
3. แก้ → compile → ขอ screenshot ยืนยันจากพี่โม่งก่อนปิดงาน
4. ห้ามรายงาน "แก้แล้ว 100%" ถ้ายังไม่เห็นภาพผลลัพธ์จริง

---

## 🔊 AUDIO PIPELINE (แก้หนี้เสียง ~50 คลิป)

| แหล่ง | ใช้ทำ | Prompt ตัวอย่าง |
|-------|-------|-----------------|
| Stable Audio | SFX ลูกชน/ลงหลุม/cushion | "Professional pool ball impact, high fidelity, sharp clack" |
| ElevenLabs | เสียงพากย์ลุงโน๊ก + โบ | เสียงผู้ตัดสินไทย "Foul — 4 points" |
| Unity Muse | Texture/เสียงใน Editor | ตามที่พี่โม่งสั่ง |

- ไฟล์ที่ได้ → วาง `Assets/CueStrike/Audio/Clips/` → ผูกเข้า AudioManager/CharacterData ทันที
- Format: `.wav` 44.1kHz 16-bit, SFX=Mono, Ambience=Stereo
- รายละเอียดเต็ม: `AUDIO_SYSTEM_PLAN.md`

---

## ✅ DEFINITION OF DONE (เช็คก่อนส่งงานทุกชิ้น)

- [ ] ตรวจไฟล์จริงด้วย tool แล้ว (ไม่เดา)
- [ ] Compile 0 errors (batchmode หรือ Test Runner)
- [ ] ไม่มีไฟล์ซ้ำ / ไม่มีไฟล์ขยะที่สร้างทิ้งไว้
- [ ] อัปเดต `CUESTRIKE_MASTER.md` + `TASK_PROGRESS.md` แล้ว
- [ ] เอกสารทุกบรรทัดที่เขียน อ้างถึงไฟล์ที่ **มีจริง** เท่านั้น
- [ ] รายงานสั้น มีหลักฐาน (path / ผล compile / screenshot)

---

## 📋 ลำดับงานถัดไป (หลังเอกสารนี้มีผล)

1. **เคลียร์บ้าน** — ลบไฟล์ซ้ำ 9 คู่ + โฟลเดอร์ขยะ (`New folder (2)`, `_Recovery`) + แก้เอกสารที่อ้างไฟล์ผี
2. **ติดตั้ง Animation Rigging** ผ่าน Package Manager (พี่โม่งกด หรือ AI ทำผ่าน MCP)
3. **Phase A Audio** — ใช้ Stable Audio/ElevenLabs เจนเสียงจริงเมื่อได้ API Key
4. **Phase B** — Animator 9 states + BoPanda Banter

---

*เอกสารนี้มีผลบังคับใช้ทันที — AI ทุกตัวที่เริ่มงานกับ CueStrike ต้องอ่านไฟล์นี้ก่อนไฟล์อื่น*
*Version 1.0 — 2026-08-06 — สร้างโดยคำสั่งพี่โม่ง*
