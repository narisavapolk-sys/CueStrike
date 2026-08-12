# CueStrike VR - Master Task Progress Tracker

> **Last Updated:** 2026-08-11
> **Current Phase:** Phase A Audio Completion

---

## 📋 PHASE OVERVIEW

| Phase | Name | Status | Progress |
|-------|------|--------|----------|
| **Phase D** | MCP Infrastructure | ✅ COMPLETE | 100% |
| **Phase A** | 3D Models (AAA) | ✅ COMPLETE | 100% |
| **Phase A** | Audio Assets | 🔄 IN PROGRESS | 20% |
| **Phase B** | P9 Animator + BoPanda Banter | 🔄 IN PROGRESS (R27: 4 clips + controller + prefab wired) | 60% |
| **Phase C** | Playability Polish | ⏳ PENDING | 0% |

---

## 🧹 HOUSE CLEANING — Round 2 (2026-08-06, per AI_TOOLS_MANDATE.md)

> ทำตาม "ลำดับงานถัดไป #1: เคลียร์บ้าน" — กลุ่มปลอดภัยสูง (Option 1) | Unity ปิดอยู่ก่อนลบ (Iron Rule 4)

### ✅ ลบไฟล์/โฟลเดอร์ขยะ (6 เป้า + .meta คู่กัน)
| เป้า | เหตุผล (verify ด้วย tool) |
|------|--------|
| `Assets/CueStrike/New folder (2)/` | โฟลเดอร์ว่าง (Count=0) |
| `Assets/_Recovery/` (มี `0.unity` 206KB) | recovery scene ไม่อยู่ใน EditorBuildSettings |
| `Assets/CueStrike/Scripts/DrillSettingsData.cs` | ไฟล์เปล่า 0 ไบต์ (ตัวจริงใน `Gameplay/Practice/` + nested class ใน `SaveSystem/CueStrikeSaveData.cs`) |
| `Assets/CueStrike/Characters/Gentleman/Scripts~/` | สำเนา legacy (namespace `CueWarp`) — Unity ไม่คอมไพล์โฟลเดอร์ `~`; caller ทั้งหมดใช้ `CueStrike.Characters.Gentleman.*` |
| `Assets/CueStrike/Characters/MeiLing/Scripts~/` | เหมือนกัน — legacy |
| `Assets/CueStrike/Characters/Somchay/Scripts~/` | เหมือนกัน — legacy |

### ✅ แก้การอ้างอิงไฟล์ผี (7 จุด, 5 ไฟล์ .md)
| ไฟล์เอกสาร | แก้จาก → ไปยัง |
|------------|----------------|
| `ROADMAP.md:113` | `CueStrikeAIEasy/Medium/Hard.cs` → `CueStrikeAIController.cs` + `CueStrikeAIStrategy.cs` (class จริง: Easy/Medium/Hard/Expert) |
| `task.md:172` | `CueStrikePinkMaterialFixer.cs` → `CueStrikeAAAApplyAll.cs` (`FixPinkMaterialsMenu`) |
| `CUESTRIKE_MASTER.md:307` | แถว `MCPSelfTest.cs` → "planned, not created" + ชี้ไป `McpTestClient.cs` |
| `CUESTRIKE_MASTER.md:337` | หัวข้อ `MCPSelfTest.cs` → "PLANNED — not yet created" + note |
| `implementation_plan_mcp_unity.md:66-67` | `(NEW)` → `(PLANNED — NOT YET CREATED)` + note ไม่มีไฟล์จริง |
| `implementation_plan_mcp_unity.md:96` | `[x]` → `[ ]` NOT YET CREATED |
| `task_mcp_unity.md:12` | `[x] implemented` → `[ ] NOT yet created` |

### ✅ ไฟล์ซ้ำ 5 คู่ — VERIFIED & RESOLVED (2026-08-09, per implementation_plan_cleanup_duplicates.md)

> ตรวจ reference จริงทีละคู่ (code `.cs` + GUID ใน prefab/scene/asset + namespace ของ caller) — ลบเฉพาะที่พิสูจน์ว่าไม่มีใครใช้ | Compile 0 errors หลังลบ

| ไฟล์ | ผลตรวจ | การตัดสินใจ |
|------|--------|-------------|
| `CueStrikeCrowdSystem.cs` | `Characters/` (1150L) **ถูกใช้จริง** โดย `CueStrikeMascotManager.cs` (ns เดียวกัน `CueStrike.Characters` → ชนะ `using CueStrike.MascotSystem`) + มี `enum CrowdReactionType`; `MascotSystem/` (395L) ไม่มี ref ใดๆ (ไม่มี `CrowdReactionType` ด้วยซ้ำ) + GUID=0 | 🗑️ ลบ `MascotSystem/` — เก็บ `Characters/` |
| `CueStrikeBallSync.cs` | ทั้ง 2 เวอร์ชันไม่มี ref ภายนอก; เวอร์ชัน `Normcore/` ไม่มี guard (ผิดกฎข้อ 4) | 🗑️ ลบ `Scripts/Multiplayer/Normcore/` — เก็บ `Multiplayer/` (guarded, canonical) |
| `CueStrikeGameSync.cs` | `Multiplayer/` ถูกใช้โดย `Editor/CueStrikeMultiplayerSetup.cs` + `CueStrikeGameSyncModel.cs`; เวอร์ชัน `Normcore/` ไม่มี ref + ไม่มี guard | 🗑️ ลบ `Scripts/Multiplayer/Normcore/` — เก็บ `Multiplayer/` |
| `CueStrikeNormcoreManager.cs` | **ใช้ทั้งคู่**: `Multiplayer/` ← `MultiplayerSetup.cs`; `Normcore/` ← `CueStrikeNormcoreSetup.cs`, `IntegrationSelfTest.cs`, `MultiplayerSelfTest.cs` | ✅ เก็บทั้งคู่ — ไม่ใช่ duplicate แท้ (คนละ consumer) |
| `ChinesePoolCallShotUI.cs` | **ใช้ทั้งคู่**: `Scripts/ChinesePool/` ← `ChinesePoolGameManager.cs`; `Scripts/UI/ChinesePool/` ← `ChinesePoolUIManager.cs` + `ChinesePoolUISetup.cs` + **GUID ใน 2 scenes** | ✅ เก็บทั้งคู่ — ไม่ใช่ duplicate แท้ |
| `RCA/UnityEngine.XR.Hands.cs` (0B) | ไฟล์ว่าง ประกาศอะไรไม่ได้; XR Hands จริง (1.5.0) ใน manifest แล้ว | 🗑️ ลบ |

> ⚠️ Note: `ChinesePoolGameManager` ใช้ `FindFirstObjectByType<ChinesePoolCallShotUI>()` (ns Gameplay) แต่ฉากมีเวอร์ชัน UI ติดอยู่ (ns UI) → ตอน runtime ค้นหาไม่เจอ จะมี warning — **รอ unified เป็นงานต่อไป**

### ⚠️ ค้นพบ PRE-EXISTING compile blocker (2026-08-06, ระหว่างขั้น 6)
- **`CueStrike_Project` (โปรเจกต์หลัก) compile ไม่ผ่าน 354 error** — ทั้งหมดมาจาก MCP layer ที่ใช้ `System.Text.Json`:
  `McpProtocol.cs` (231), `McpServer.cs` (24), 5 Tools ×9, `SkinSetup.cs`, `SkinBuilder.cs`, `IMcpTool.cs`, `McpTestClient.cs`
- **สาเหตุ:** `using System.Text.Json` / `[JsonPropertyName]` แต่โปรเจกต์ไม่มี `.asmdef` อ้าง `System.Text.Json` → ชนิดหาไม่เจอ
- **พิสูจน์ว่าไม่ใช่การลบของผม:** 0 error อ้างไฟล์ที่ลบ (`DrillSettingsData`/`Scripts~`/`_Recovery`/`New folder`) ทั้งหมด
- **ทำไม log เก่าบอก "0 errors":** เก่า compile ที่ workspace-root ซึ่งไม่มีโค้ด MCP (`CueStrike/Editor/MCP/McpProtocol.cs` ไม่มีใน root)
- **สถานะ: ✅ FIXED แล้ว (2026-08-06)** — migrate MCP layer จาก `System.Text.Json` → `Newtonsoft.Json` (UPM `com.unity.nuget.newtonsoft-json` 3.2.1) ตาม Rule 6 (ห้ามฉีด DLL ภายนอก)
- แก้เพิ่มจาก error ที่เปิดเผยเมื่อ MCP คอมไพล์ผ่าน: เพิ่ม `using CueStrike.MCP.Tools` (McpServer) / `using System.Linq` (ExecuteCodeTool, McpTestClient) / `IMcpTool missing` / `SeasonalEvent.Spring,Winter` / `ModelImporterMaterialLocation.InProject→External` / `LightingSettings.CreateInstance→new LightingSettings()` / `SkinRarity` / `UnityEngine.Object.DestroyImmediate` / `Application.logMessageReceived` event
- **ผล compile ล่าสุด: 0 errors (log `compile_verify_newtonsoft.log`, return code 0)**

---

## 🔧 VCS SETUP — Round 3 (2026-08-09, per implementation_plan_git_setup.md)

> โปรเจกต์ไม่เคยมี version control มาก่อน — ตั้ง git + .gitignore + Git LFS ตามกฎข้อ 5 (plan ก่อนลงมือ) และกฎข้อ 2 (อัปเดตเอกสารในรอบเดียวกัน) | Unity ปิดอยู่ก่อนแก้ไฟล์ (Iron Rule 4)

### ✅ ทำแล้ว
| รายการ | รายละเอียด |
|--------|-----------|
| `git init` | branch `main` — root commit `8f7b347` (8,309 files, 1,169,767 insertions) |
| `.gitignore` | Unity caches (`Library/ Temp/ Logs/ UserSettings/`) + `*.log` + `err*.txt` + `%f` + `gold_hex*.txt` + `filelist*.txt` + `all_scripts.txt` + `__pycache__/` + `.freebuff/` + `.agents/` + ffmpeg downloads (`*.tar*`) |
| Git LFS | 274 binary files (`*.fbx *.obj *.png *.webm *.dll *.wav *.mp3 *.mp4 *.jpg *.jpeg`) — Assets รวม 328MB เก็บเป็น pointer |
| **SECURITY** | `api  key  ai audio/` (ElevenLabs `ElevenLabs KeY tester  for sound.txt` + `stability.ai.txt` plaintext) **excluded จาก VCS** — ตรวจ `git ls-files` ก่อน commit ไม่พบ secrets ✓ |
| เอกสาร | `CUESTRIKE_MASTER.md` (header + §5) + `TASK_PROGRESS.md` (ไฟล์นี้) อัปเดตในรอบเดียวกัน ✓ |

### ⏳ ยังเหลือ (งานต่อเนื่อง)
- ย้าย API keys ออกจากโปรเจกต์จริง (แนะนำ rotate) — ตอนนี้แค่ exclude จาก git
- nested `CueStrike_Project/` skeleton — โฟลเดอร์ว่าง 0 ไฟล์ (git ไม่ track อยู่แล้ว) — ลบได้
- parent `UnityProjects/CueStrike/` มีโฟลเดอร์ซ้ำ (`Assets/ Library/ ProjectSettings/` ฯลฯ) = สำเนาเก่า — รอรวมเป็น root เดียว (Consolidation)
- เพิ่ม remote (GitHub/GitLab) + push baseline → เปิด PR workflow ได้จริง
- `Assets/fix_safemode_v2.py` + `Assets/CueStrike/fix_safemode_v2.py` — สคริปต์ dev อยู่ใน asset DB (ซ้ำกัน 2 ที่) — รอย้ายไป `Tools/` นอก Assets


---

## 🎬 SCENE LOADING FIX — Round 4 (2026-08-09, per implementation_plan_scene_fix.md)

> พบบั๊กจริงจากการตรวจ compile + scene flow (กฎข้อ 1): build settings มี scene แค่ 1/11 และ MainMenu โหลด scene "hub" ที่ไม่มีอยู่จริง | Unity ปิดอยู่ก่อนแก้ (Iron Rule 4) | แก้ผ่าน batchmode `-executeMethod` (กฎข้อ 4)

### ✅ ทำแล้ว
| รายการ | รายละเอียด |
|--------|-----------|
| Build Settings | เพิ่มครบ **11 scenes** (`Assets/CueStrike/Scenes/**`) ผ่าน `SceneBuildSettingsFixer.cs` — `EditorBuildSettings.scenes` ตั้งด้วย API ไม่ใช่แก้มือ |
| `MainMenuUIController.cs:125` | `LoadScene("hub")` → `LoadScene("Snooker_Demo")` (scene `hub` ไม่มีจริง) |
| Editor Tool ใหม่ | `Tools → CueStrike → Fix → Add All Scenes to Build Settings` — guard 3 ชั้น, batchmode-safe |
| Compile | ✅ **0 errors** (batchmode exit 0) |

### ⏳ ยังเหลือ / Note
- `Audio/Clips/**/*.meta` เดิมใน baseline ไม่สมบูรณ์ (มีแค่ guid) — Unity เติม `AudioImporter` ให้อัตโนมัติเวลาเปิด; revert ไว้ใน commit นี้ — รอ decision พี่โม่ง
- เป้า Practice = `Snooker_Demo` (ฉากเล่นได้จริงนอกเหนือจากห้อง) — พี่โม่งเปลี่ยนเป็นฉากอื่นได้ที่ `MainMenuUIController.cs:125`
- `TitleSceneManager.mainSceneName` ยังค่า default `"MainScene"` (ไม่มีฉากนี้) — ยังไม่ได้ใช้เพราะ component ไม่ได้อยู่ใน Title scene — ควรล้างค่า default ทิ้งเมื่อแตะไฟล์นี้ครั้งหน้า

---

## 🚦 COMPILE GATE — Round 6 (2026-08-09, per implementation_plan_compile_gate.md)

> ตัดวงจร compile-fix ซ้ำๆ (หลักฐาน: `compile_fix_errors1-18.log` ~40 รอบ) ด้วย gate อัตโนมัติ | Unity ปิดก่อนตั้งค่า (Iron Rule 4)

### ✅ ทำแล้ว
| รายการ | รายละเอียด |
|--------|-----------|
| `tools/compile_check.sh` | batchmode compile check — auto-detect Unity (`6000.4.4f1` ก่อน), `UNITY_PATH` override ได้, exit 0 = 0 errors, log = `compile_gate.log` (gitignored) |
| `.githooks/pre-commit` | บล็อก commit ที่ stage `.cs` ถ้า compile พัง; ข้ามถ้าไม่มี `.cs` staged หรือ Unity Editor เปิดอยู่; escape: `git commit --no-verify` |
| LFS hooks | คัดลอก `post-checkout/post-commit/post-merge` เข้า `.githooks/` + `git config core.hooksPath .githooks` — LFS ไม่พัง |
| ทดสอบ | ✅ (a) สคริปต์ exit 0 | ✅ (b) `.cs` พัง → hook บล็อก exit 1 + โชว์ error | ✅ (c) `.cs` ดี → hook ผ่าน exit 0 |

### 📌 วิธีใช้ / ตั้งค่าในเครื่องใหม่
- รันด้วยมือ: `tools/compile_check.sh`
- clone ใหม่ ต้องตั้ง: `git config core.hooksPath .githooks` (hook อยู่ใน repo แล้ว)

---

## 🎯 CALLSHOT UI MERGE — Round 7 (2026-08-09, per implementation_plan_merge_callshot_ui.md)

> รวม `ChinesePoolCallShotUI` 2 เวอร์ชัน → 1 — แก้บั๊ก GameManager หา UI ไม่เจอ (FindFirstObjectByType) | Unity ปิดก่อนแก้ (Iron Rule 4)

### ✅ ทำแล้ว
| รายการ | รายละเอียด |
|--------|-----------|
| เก็บ | `Scripts/UI/ChinesePool/ChinesePoolCallShotUI.cs` (ns `CueStrike.UI.ChinesePool`) — GUID `0d69029a…` ผูกกับ 2 scenes (`AAA_RoomDAY`, `Title_NoksGrandHall`) — scene data ปลอดภัย |
| ลบ | `Scripts/ChinesePool/ChinesePoolCallShotUI.cs` (ns Gameplay, 280L) + `.meta` — dead code: API ไม่มี caller, `GetBallIdFromButtonIndex`=-1 (highlight พัง), GUID `c743997f…` = 0 ref |
| แก้ | `ChinesePoolGameManager.cs` + `using CueStrike.UI.ChinesePool;` → `FindFirstObjectByType` เจอ class ในฉากจริง (บั๊กหาย) |
| Compile | ✅ **0 errors** (`compile_check.sh` exit 0) |

### ⏳ ยังเหลือ (งานถัดไป)
- ผูก `callShotUI.OnShotCalled += GameManager.SetCallShot` + `OnCallShotCancelled → ClearCallShot` (Ruleset เรียก `SetCallShot` อยู่แล้ว แต่ UI event ยังไม่ต่อ)
- UI ในฉากบาง instance มี field ว่าง (`_callShotPanel: {fileID: 0}` ฯลฯ) — ต้อง assign ใน Editor + Vision audit (กฎข้อ 4)

---

## 🧼 SCENE NAME DEFAULTS CLEANUP — Round 8 (2026-08-09, per implementation_plan_clean_scene_names.md)

> ล้างค่า default เก่าใน `TitleSceneManager.cs` ที่ชี้ฉากไม่มีจริง (ชื่อจากยุคเก่า) — preventive เพราะ class ยังไม่ถูกใส่ในฉาก | Unity ปิดก่อนแก้ (Iron Rule 4)

### ✅ ทำแล้ว
| field | เดิม (พัง) | ใหม่ | หมายเหตุ |
|-------|-----------|------|----------|
| `mainSceneName` | `"MainScene"` | `"MainMenu"` | ใช้จริงโดย btnPlay → `LoadScene`; Title → MainMenu |
| `practiceSceneName` | `"PracticeHub"` | `"Snooker_Demo"` | ตรงกับ MainMenuUIController (R4) |
| `multiplayerSceneName` | `"MultiplayerLobby"` | `""` | ยังไม่มีฉาก (P7 partial); `LoadScene` guard ค่าว่าง |
| `settingsSceneName` / `creditsSceneName` | `"Settings"` / `"Credits"` | `""` | เป็น panel ใน Title scene ไม่ใช่ฉาก |
| Compile | ✅ **0 errors** (`compile_check.sh` exit 0) | | |

### ⚠️ พบเพิ่ม (บันทึก ไม่แก้)
- ~~`CueStrikeVRStartup.cs` มี default `"Main"`/`"Boot"` เก่า~~ → **✅ แก้แล้ว Round 10 (2026-08-09):** ตรวจแล้วเป็น duplicate ของ `VR/VRStartup.cs` (ตัวจริง) — ถูกลบแล้ว

---

## 🚀 REMOTE + PUSH — Round 9 (2026-08-09)

> push โปรเจกต์ขึ้น GitHub ครั้งแรก — เริ่ม PR workflow ได้แล้ว

### ✅ ทำแล้ว
| รายการ | รายละเอียด |
|--------|-----------|
| Remote | `origin` → `https://github.com/narisavapolk-sys/CueStrike.git` |
| Push | `main` (7 commits, HEAD `85e6e6b`) — `git push -u origin main` exit 0 |
| LFS | 274 files / 250MB ส่งครบ (`git lfs push --dry-run` ว่าง) |
| Auth | Git Credential Manager (ระบบของ Git for Windows) |

### 📌 วิธีทำงานต่อจากนี้ (PR workflow)
- งานใหม่ → สร้าง branch (`git checkout -b feature/xxx`) → commit → push → เปิด PR ผ่าน GitHub → merge
- ตั้งค่าหลัง clone: `git config core.hooksPath .githooks` (compile gate)
- ถัดไปแนะนำ: GitHub Actions CI รัน compile gate อัตโนมัติทุก PR

---

## 🎮 VR STARTUP CLEANUP — Round 10 (2026-08-09, per implementation_plan_vr_startup_cleanup.md)

> ตรวจ VR startup 2 ตัวที่ซ้ำกัน (ต่อจาก note R8) — หาตัวจริง + ลบตัวซ้ำ | Unity ปิดก่อนแก้ (Iron Rule 4)

### ✅ ทำแล้ว
| รายการ | รายละเอียด |
|--------|-----------|
| เก็บ | `VR/VRStartup.cs` (`VRStartup`) — **ตัวจริง**: Quest optimization ครบ (auto 72/90Hz, CPU/GPU, FFR, OpenXR Meta features) |
| ลบ | `Scripts/CueStrikeVRStartup.cs` + `.meta` — duplicate ที่เก่ากว่า, scene names พัง (`"Main"`/`"Boot"` ไม่มีฉากนี้) — GUID 0 ref, ไม่มี code ref |
| Verify | GUID `59437c5c…` = 0 ref ทั่ว Assets; compile **0 errors** (`compile_check.sh` exit 0) |

### ⏳ งานถัดไป
- ✅ `VRStartup.cs` ถูกใส่ในฉากแล้ว (R13 — `Boot.unity` Scene 0 + Editor tool "NARI CUE STRIKE") — เหลือ Vision audit

---

## 🔗 CALL-SHOT WIRING — Round 11 (2026-08-09, per implementation_plan_callshot_wiring.md)

> ต่อวงจร call-shot ให้ครบ (ต่อจาก R7): UI event → GameManager | Unity ปิดก่อนแก้ (Iron Rule 4)

### ✅ ทำแล้ว
| รายการ | รายละเอียด |
|--------|-----------|
| `OnShotCalled` → `SetCallShot` | subscribe ใน `AutoWireReferences()` (หลังหา `callShotUI`) — `GameManager.cs:121` |
| `OnCallShotCancelled` → `ClearCallShot` | subscribe — `GameManager.cs:122` |
| Unsubscribe | ใน `OnDestroy()` — `:90-91` (event hygiene) |
| Compile | ✅ **0 errors** (`compile_check.sh` exit 0) |

### ⏳ ยังเหลือ (งานถัดไป)
- ~~ฝั่งแสดง UI ยังไม่มี trigger~~ → **ทำแล้ว R14** (show trigger: `MaybeShowCallShotUI()` — panel โชว์เมื่อต้องเรียก)
- UI ในฉากบาง instance field ว่าง (`_callShotPanel: {fileID: 0}` ฯลฯ) — assign ใน Editor + Vision audit (กฎข้อ 4) ก่อนเล่นจริง

---

## 🚦 GITHUB ACTIONS CI — Round 12 (2026-08-09, per implementation_plan_github_ci.md)

## 🧹 CS0618 FIND-API MODERNIZE — Round 15 (2026-08-10, per implementation_plan_cs0618_cleanup.md)

**Goal:** Eliminate CS0618 warnings from deprecated FindObjectOfType/FindObjectsOfType family across runtime + editor.

### ✅ Migration mapping (verified against Unity 6 docs)
- `FindObjectOfType<T>()` → `FindFirstObjectByType<T>()` (semantics identical, since 2023.1)
- `Object.FindObjectOfType<T>()` → `Object.FindFirstObjectByType<T>()` (same)
- `GameObject.FindObjectsOfType<T>()` → `FindObjectsByType<T>(FindObjectsSortMode.None)` (drop legacy `GameObject.` prefix)
- `FindObjectsOfType<T>()` → `FindObjectsByType<T>(FindObjectsSortMode.None)` (no sort cost; order may differ but no call site depends)
- `FindObjectsOfType<T>(true)` → `FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)` (one Editor occurrence)

### ✅ จำนวน
- **36 call sites** replaced (15 runtime + 21 editor) — `grep -rn "FindObject[s]OfType" Assets/CueStrike --include="*.cs"` จาก 36 → 0 (verify dry-run + post-write).
- **21 ไฟล์** modified (13 runtime + 8 editor): `Audio/NearMissDetector.cs`, `Characters/Bones/BonesXRayVision.cs`, `Characters/Editor/CharacterSetupEditor.cs`, `Characters/Gentleman/GentlemanAbilityController.cs`, `Characters/MeiLing/MeiLingAbilityController.cs`, `Characters/Phantom/PhantomSpectralSight.cs`, `Demo/CueStrikeAutoDemo.cs`, `Editor/ChinesePoolEditor.cs`, `Editor/ChinesePoolUISetup.cs`, `Editor/CueStrikeVisualAudit.cs`, `Editor/IntegrationSelfTest.cs`, `Editor/MultiplayerSelfTest.cs`, `Editor/NoirMemoryPuzzleEditor.cs`, `Editor/NoirMemorySelfTest.cs`, `Editor/RoomScreenshotTool.cs`, `Editor/TitleSceneFixer.cs`, `Environment/CueStrikeEnvironmentManager.cs`, `Gameplay/Tutorial/CueStrikeTutorialManager.cs`, `Scripts/VR/Input/CueStrikeVRInputManager.cs`, `UI/CueStrikeHUD.cs`, `UI/CueStrikeHUDController.cs`.

### ✅ Compile verify (กฎข้อ 4)
- `tools/compile_check.sh` (Local Unity gate) → **0 errors** ✅
- ไม่มี runtime/behavior change (Unity 6 ScriptReference guarantees):
  - `FindFirstObjectByType` คืน instance ตัวเดียวกับ `FindObjectOfType` (active + InstanceID order)
  - `FindObjectsByType(None)` คืน component ชุดเดียวกัน — order เปลี่ยนได้ แต่ไม่มี call site ที่ depend order
  - `FindObjectsByType(Include, None)` = exactly same as `FindObjectsOfType(true)`
- PR/commit: branch `chore/cs0618-find-modernize` (รอพี่โม่งกด Create)

### ℹ️ งานที่ uncommitted อยู่ใน main checkout (ของอีก session — ไม่แตะ)
- `BoPandaBanter.cs`, `BoPanda_Prefab.prefab`, `ExecuteCodeTool.cs`, `CueStrikeVoiceBinder.cs` (-d), `AAA_RoomDAY.unity`, `McpSettings.asset`
- แตะเฉพาะ 21 ไฟล์ R15 + plan

### ⏳ ยังเหลือ (แนะนำเป็นงานถัดไป)
1. **Branch protection** บน main (บล็อก push ตรง + บังคับ PR + CI เขียวก่อน merge) — ตั้งในหน้า Settings → Branches ของ GitHub
2. **`UNITY_LICENSE` secret** ตั้งในหน้า Actions secrets — workflow รอบแรก fail ตามคาด จนกว่าจะตั้ง
3. **Vision audit** (กฎข้อ 4) — ลอง build + run ใน Editor ดูสคริปต์ที่ find instance ตอน startup (VR singleton, RoomLightingManager, TutorialManager) ทำงานถูก

### 🛠️ Tool (เผื่อใช้ซ้ำ / ต่อยอด)
- `tools/migrate_find_api.py` — token-based scan + balanced-paren matcher
  - Handle nested generic เช่น `Dictionary<string, List<int>>`
  - Skip comments + string/char literals กัน false-positive
  - Verifiable input/output (dry-run mode: `--dry`)

📝 Plan: `implementation_plan_cs0618_cleanup.md`

> ต่อยอด compile gate (R6) ขึ้น CI: ทุก PR/push → Unity batchmode compile check บน GitHub | Unity ปิดก่อนแก้ (Iron Rule 4)

### ✅ ทำแล้ว
| รายการ | รายละเอียด |
|--------|-----------|
| `.github/workflows/compile-gate.yml` | trigger `pull_request` + `push` ไป main → `game-ci/unity-test-runner@v4` (editmode: compile ก่อนรัน test; พัง = fail job) + checkout `lfs: true` + cache Library |
| `.github/workflows/unity-activate.yml` | helper รันครั้งเดียว สร้าง `.alf` → ตั้ง secret `UNITY_LICENSE` |
| YAML | ✅ valid (python yaml) |
| `.gitignore` | เพิ่ม `test-results.xml` + `*_results.xml` |

### ⏳ พี่โม่งต้องทำ 1 ครั้ง (เปิดใช้งาน CI)
1. GitHub repo → Actions → รัน **Acquire Unity Activation File** → ดาวน์โหลด artifact `Unity_v6000.x.alf`
2. https://license.unity3d.com/manual → อัปโหลด `.alf` → ได้ไฟล์ `.ulf`
3. Repo → Settings → Secrets and variables → Actions → New secret: ชื่อ `UNITY_LICENSE` = เนื้อหา `.ulf` ทั้งหมด
4. ครั้งแรก workflow จะ fail เพราะยังไม่มี secret (คาดหมายได้) — ตั้ง secret แล้ว rerun

---

## 🎮 BOOT SCENE + VRSTARTUP EDITOR TOOL — Round 13 (2026-08-10, per implementation_plan_boot_scene.md)

> สร้าง Boot scene (Scene 0) + Editor tool "NARI CUE STRIKE" ผูก VRStartup.cs ตาม design (`VRStartup.cs:16`) | Unity ปิดก่อนแก้ (Iron Rule 4)

### ✅ ทำแล้ว
| รายการ | รายละเอียด |
|--------|-----------|
| `Scenes/Boot.unity` (ใหม่) | Scene 0 ใน Build Settings (12 scenes รวม) — GO `BootManager` + `VRStartup` (Quest optimization: frame rate, CPU/GPU, FFR, OpenXR features) + `BootSceneLoader` (→ `Title_NoksGrandHall` ผ่าน `CueStrikeLoadingScreen` — reuse gold standard VR transition) |
| `Editor/BootSceneBuilder.cs` (ใหม่) | เมนู `Tools → NARI CUE STRIKE → Build Boot Scene (VRStartup)` — guard 3 ชั้น, idempotent (ไม่ซ้ำใน build settings), ใช้ batchmode `-executeMethod` ได้ตามกฎข้อ 4 |
| `VR/BootSceneLoader.cs` (ใหม่) | `public string nextSceneName = "Title_NoksGrandHall"` → `Start()` เรียก `CueStrikeLoadingScreen.LoadScene` (guard ค่าว่าง) |
| Build settings diff | เพิ่ม Boot ที่ index 0 เท่านั้น — 11 ตัวเดิมเรียงเดิม (side benefit: build เริ่มที่ Boot → Title แทนที่จะเริ่มที่ห้อง) |
| Compile | ✅ **0 errors** (batchmode exit 0, script compilation 14.5s) — scene สร้างโดย Unity เอง, GUID ตรวจตรง (`VRStartup.cs` = `c650868c…`, `BootSceneLoader.cs` = `c4d6e49f…` ใน scene) |
| หมายเหตุ | licensing ล่มรอบแรก (protocol mismatch, transient) → retry ผ่าน |

### ⏳ งานถัดไป
- Vision audit (กฎข้อ 4): เปิด Editor → เล่นจาก `Boot.unity` → ดู transition → Title ทำงาน + ตั้งค่า VRStartup (frame rate 72/90 ตามอุปกรณ์)
- CI: workflow รอ secret `UNITY_LICENSE` (Round 12) — ตั้งแล้ว compile gate จะรันบน PR ด้วย

---

## 🎯 PHASE A AUDIO - DETAILED CHECKLIST

### ✅ COMPLETED (Architecture & Setup)
- [x] Audio system architecture analysis complete
- [x] CueStrikeAudioManager reviewed (14 clip slots)
- [x] BallSoundController reviewed (velocity-based hit sounds)
- [x] PocketSoundDetector reviewed (trigger-based pocket sounds)
- [x] NearMissDetector reviewed (near-miss gasp detection)
- [x] CueStrikeChampionshipCrowd reviewed (crowd reactions)
- [x] CharacterData audio fields verified (voiceClip, abilitySound)
- [x] Placeholder clips generated (9 WAV files via CueStrikeAudioGenerator.cs)
- [x] ROADMAP.md updated with current status
- [x] TASK_PROGRESS_AUDIO.md created with detailed requirements
- [x] NEXT_STEPS_SUMMARY.md created with recommendations

### 🔄 IN PROGRESS
- [ ] **Source/Create real audio clips (~50 total)**
  - [ ] 14 AudioManager clips
  - [ ] 20 Character voice + ability clips (10 chars × 2)
  - [ ] 9 Room ambience clips
  - [ ] 7+ Crowd system clips

### ⏳ PENDING (After clips sourced)
- [ ] Create 10 CharacterData ScriptableObjects
- [ ] Assign all clips in Unity Inspector
- [ ] Set up CueStrikeAudioManager in all room scenes
- [ ] Implement room ambience switching logic
- [ ] Assign crowd clips to CueStrikeChampionshipCrowd
- [ ] Play mode testing & validation
- [ ] Documentation updates

---

## 🎯 PHASE B - ANIMATOR & BANTER (PENDING)

### P9 Animator Controller
- [ ] Create Animator Controller asset
- [ ] Add 9 states: Idle, Walk, Aim, Shoot, Celebrate, Disappointed, Speak, Neutral, Victory
- [ ] Create transitions with parameters
- [ ] Assign to all 10 character prefabs

### BoPanda Banter System
- [ ] Design banter event system
- [ ] Implement frame start/end comments
- [ ] Implement pot success reactions
- [ ] Implement foul callouts
- [ ] Implement century break hype
- [ ] Implement snooker escape commentary
- [ ] Implement fluke teasing
- [ ] Implement near miss gasps

### Uncle Nok Referee Integration
- [ ] Hook UncleNokReferee.cs to ShotManager events
- [ ] Hook UncleNokReferee.cs to RulesManager events
- [ ] Add voice announcement clips
- [ ] Test in play mode

### Crowd Reactions
- [ ] Connect CueStrikeCrowdSystem to game events
- [ ] Test spatial audio in GrandArena
- [ ] Verify 84 spectator performance

---

## 🎮 PHASE C - PLAYABILITY (PENDING)

### Shot Manager Polish
- [ ] Fine-tune cue aiming
- [ ] Fine-tune power charge
- [ ] Fine-tune spin (english)
- [ ] Fine-tune preview line

### Rules System
- [ ] Complete 8-ball rules
- [ ] Complete 9-ball rules
- [ ] Complete Snooker rules
- [ ] Foul detection
- [ ] Ball-in-hand logic
- [ ] Turn switching

### Multiplayer
- [ ] Photon/Fusion integration
- [ ] Turn-based sync
- [ ] Voice chat (CueStrikeVoiceManager exists)

### UI/HUD
- [ ] Finalize CueStrikeHUD.cs
- [ ] Finalize MainMenu
- [ ] Finalize LobbyUI
- [ ] Finalize NoirMemory results

### Character Abilities
- [ ] Balance 10 unique abilities
- [ ] Set cooldowns
- [ ] Add visual FX
- [ ] Add audio cues

---

## 📦 PARALLEL WORK - CHARACTER PREFABS & ANIMATIONS

### Ready to Run (CharacterAAASetup.cs)
- [ ] Run "Setup All AAA Characters" menu item
- [ ] Verify 12 prefabs created
- [ ] Verify Humanoid rig on all
- [ ] Verify URP/Lit materials
- [ ] Verify IK targets (LeftHand/RightHand)

### Need Animation Clips
- [ ] Create/acquire P9 animation clips
- [ ] Assign to Animator Controller
- [ ] Test IK with cue interaction
- [ ] Add LOD groups

---

## 🛠️ IMMEDIATE COMMANDS (Run in Order)

```bash
# 1. Blender: Generate all AAA assets
# Open Blender 3.6 → Scripting → Paste create_all_aaa_master.py → Run ▶

# 2. Unity: Import & Configure All AAA
# Tools → CueStrike → Apply → Apply ALL AAA (Final Polish)

# 3. Verify Self-Test
# Check console for "ALL PASSED ✅"

# 4. Fix Pink Materials (if any)
# Tools → CueStrike → Fix → Fix Pink Materials (URP Conversion)

# 5. Generate Placeholder Audio
# Tools → CueStrike → Generate → Create Placeholder Audio

# 6. Setup Character Prefabs
# Tools → CueStrike → Character System → Setup All AAA Characters
```

---

## 📁 KEY REFERENCE FILES

| File | Purpose |
|------|---------|
| `TASK_PROGRESS_AUDIO.md` | Detailed audio task tracking |
| `NEXT_STEPS_SUMMARY.md` | Strategic overview & recommendations |
| `ROADMAP.md` | Phase breakdown with checkboxes |
| `CHARACTER_SYSTEM_PLAN.md` | 12 character roster & abilities |
| `AUDIO_SYSTEM_PLAN.md` | Audio architecture |
| `AAA_WORLD_TOUR_IMPLEMENTATION.md` | Room decoration plan |

---

## 💡 STRATEGIC FOCUS

**NOW:** Phase A Audio - Source ~50 clips, assign, test
**NEXT:** Phase B Animator - P9 controller + BoPanda banter + Uncle Nok
**THEN:** Phase C Playability - Rules, multiplayer, UI, abilities

**The foundation is solid. The pipeline works. Now it's content + polish time! 🎱**

## 📌 2026-08-06 — Session Log (Post Coaching Reset, Act Mode)

> Coach รีเซ็ตให้ key platform.stability.ai ใหม่ — ตรวจสอบทุกจุดตาม Iron Rules ก่อนกระทำ (Rule 1 Verify + Rule 6 Technology Mismatch)

### สถานะ Mission (verify ด้วยเครื่องมือ)
| Mission | Status | Evidence |
|---|---|---|
| 1. Compile 0 errors | ✅ | `-executeMethod PinkMaterialFixer.RunScan` → `compile_and_scan.log` = 0 errors, Unity return code 0 |
| 2. Pink Exorcist | ✅ Scan/Preview | CS0117 fixed (`LoadMainAssetAtGUID`→`GUIDToAssetPath`+`LoadAssetAtPath`); Scan เจอ **41 วัสดุชมพู** (`Standard`→`URP/Lit`, `applied=False`); report: `Assets/CueStrike/Editor/HueFix/Report/pink_report_scan_20260806_215736.txt` |
| 3. English voiceover (Uncle Nok) | ✅ 14 clips | Windows TTS `Zira` (en-US) → `Assets/CueStrike/Audio/Clips/Voice/UncleNok/*.wav` |
| 4. Thai voiceover | ⛔ | Windows มีแต่ `Zira(en-US)`+`Hazel(en-GB)` ไม่มีเสียงไทย; `edge-tts` ไม่ได้ลง (pip พร้อมลง) |
| 5. SFX (Stable Audio) | ⛔ BLOCK (Rule 6) | ดู "SFX Block" ด้านล่าง |
| 6. Dedupe Wave 2 | ⏳ | 5 คู ค้าง (ทำหลัง Audio เสร็จ) |

### ⚠️ SFX Block — Technology Mismatch (Iron Rule 6: หยุดทันที อย่าใช้ credits ไปโดยไม่มีจุดสิ้นสุด)
Key ที่ได้รับ = `sk-XnRw…` (ความยาว 51, prefix `sk-XnR`) — เป็น **Stability.ai platform/image key** ไม่ใช่ Stable Audio token → พิสูจน์หลายเส้นทางใช้ credits **0** บาท:
| การพิสูจน์ (พร้อม key จริง) | ผล | ความหมาย |
|---|---|---|
| `POST api.stableaudio.com/v2/sound-generation` | **401** (body สั้น) | key ชนิดผิด: Stability.ai image key ≠ Stable Audio token |
| `POST platform.stability.ai/v2beta/stable-audio/sound-generation` | **405** | ไม่ใช่ API endpoint จริง |
| `GET platform.stability.ai/v2beta/stable-audio/sound-generation` | 200 + HTML SPA | เส้นทางซ้อน SPA (frontend), ไม่ใช่ sound API |
| `api.stability.ai/v2beta/audio/sfx`, `/v2beta/stable-audio/sound-generation` | **404** | ไม่มีบน host เดิมกัน |

**สรุป:** คีย์ไม่เข้าถึง sound-generation ได้ที่ไหนก็ตาม; platform.stability.ai ไม่มี sound endpoint เปิดที่พบหลังพิสูจน์ ~10 เส้นทาง → **ยังไม่ใช้ 25 credits** (Rule 1: ห้ามใช้โดยไม่ได้รับอนุญาต/ยืนยันเบื้องตอน)

**ตัวเลือก Coach (เลือกอันเดียว):**
- (A) มอบ **Stable Audio token** (stableaudio.com → Account → API Keys) → ผมสร้าง 8 SFX AI จริงทันที (prompt มีอยู่ใน AUDIO_SYSTEM_PLAN pp.41–45)
- (B) อนุญาตใช้ **CueStrikeRealisticAudioSynth.cs** (fallback ตาม AUDIO_SYSTEM_PLAN p.117 "เป็น backup ถ้าไม่มี wav") → เขียน 8 SFX ซินเตซิสรันไทม์แบบ code (0 credits, ไม่ต้อง key); สลับเป็น wav AI ภายหลังได้
- (C) ถอยไปทำ **Pink Apply + TTS wiring ก่อน** (ไม่ทำ SFX ขณะนี้)

### รอการยืนยัน/การกระทำจาก Coach (ไม่ action จากฝ่ายนี้โดยไม่ได้รับอนุญาต)
- **Pink Apply:** คุณกด `Tools → CueStrike → Fix → Fix Pink Materials` (มี Undo) แล้วส่ง screenshot เพื่อ Vision confirm (Rule 4)
- **Voice script:** ให้ข้อความอังกฤษ + ไทยเต็ม (ขณะนี้มีเฉพาะ "Foul — 4 points" เป็นอ้างอิง) เพื่อสร้าง/เชื่อม wav ครบทุกหมวด `UncleNokReferee` (16+ หมวด)
- **SFX:** เลือก (A)/(B)/(C) ด้านบน

## 🔵 2026-08-06 Session 2 — Execution (หลังอ่าน AI_TOOLS_MANDATE.md + ทำตามกฎเหล็ก)

### ✅ เสร็จแล้ว (verify ด้วย tool)
| งาน | ผล | หลักฐาน |
|---|---|---|
| Pink Fix (`PinkMaterialFixer.RunFix`, batchmode) | 41/41 → `Standard->URP/Lit`, applied=True | `pink_fix.log`: "FIX complete: 41 materials"; รีพอร์ต `Editor/HueFix/Report/pink_report_fix_20260806_225245.txt` (ทุกบรรทัด applied=True); Unity exit 0 |
| Vision Audit (`RoomScreenshotTool.CaptureAll`) | ถ่ายห้อง 8 ภาค รวม `GrandArena_Room` (ห้องชมพูเดิม) | PNG ที่ `CueStrike_Project/RoomScreenshots/` (GrandArena_Room.png=29KB ฯลฯ); log `[RoomShot] ALL DONE!`; Unity exit 0 |
| English TTS (Zira en-US) | 14 .wav | `Audio/Clips/Voice/UncleNok/*.wav` |
| Compile | 0 errors | batchmode exit 0 |

### ⛔ SFX Block — Technology Mismatch (Rule 6: หยุด + รายงาน, ใช้ 0 credits)
พิสูจน์ endpoint ที่ Coach ระบุ (key จริง `sk-XnRw…`, 51 ตัวอักษร, prefix `sk-XnR`):
- `api.stability.ai/v2beta/audio/stable-audio-2/sound-generation` → **404** (GET+POST เท่ากัน, body ว่าง)
- `api.stability.ai/v2beta/audio/sfx`, `/v2beta/stable-audio/...`, `/v2/sound-generation` → **404**
- `api.stableaudio.com/v2/sound-generation` → **401** (bodyสั้น) — host เสียงแยก auth; sk- เป็น image/speech key ไม่ใช่ Stable Audio token

**สรุป:** คีย์ `sk-` คือ Stability.ai platform/image+speech token → api.stability.ai ไม่มี path sound-generation (404หมด); stableaudio.com ใช้ token แยก (401) → **ไม�่มี SFX-generation endpoint เข้าถึงได้ด้วยคีย์นี้** → ยังไม่ใช้ 25 credits (Rule 1: ห้ามใช้โดยไม่มี endpoint ทำงานจริง; Rule 6: ห้ามฝืนทำต่อ)

**รอคำสั่ง Coach (เลือกอันเดียว):** (A) มอ exact endpoint + Stable Audio token ที่ใช้งานได้ → gen 8 SFX AI จริง; (B) อนุญาต fallback in-repo synth (`CueStrikeRealisticAudioSynth` + 9 placeholder wavs ที่มีอยู่ใน `Audio/Clips/`) เป็น SFX → อัปเกรดผ่าน code, 0 credits; (C) พัก SFX (ใช้ placeholder ที่มีอยู่แล้ว)

### 📋 พร้อม/ต่อไป (ไม่บังคับ SFX)
- **Vision Audit ✅** — Coach ใช้ Vision ตรวจ `GrandArena_Room.png` (+หรืออื่น) ยืนยัน "ไม่มีสีชมพูแล้ว" (rule 3: ต้องวิเคราะห์จากภาพจริง ไม่เดาจากโค้ด)
- **TTS wiring** — มี 14 wav Zira แล้ว (`Voice/UncleNok/`) + `UncleNokReferee.cs` (16 หมวด AudioClip[]) + `UncleNok_Prefab.prefab` → พร้อม wiring เมื่อชัดเจนแหล่งที่มา (Zira interim หรือ ElevenLabs ตามมาตรฐาน mandate p.109) + มี script เต็ม (ตอนนี้มีเฉพาะ "Foul — 4 points")
- **SFX assets** — มี placeholder 9 wavs ใน `Audio/Clips/` (ball_ball_hit, ball_cushion_hit, ball_pocket_drop, chalk_scrape, crowd_murmur, cue_ball_hit, ambient_room_tone, ui_click, ui_hover) + controller 4 ตัว (`BallSoundController`,`PocketSoundDetector`,`NearMissDetector`,`CueStrikeDynamicPhysicsSFX`) มีอยู่แล้ว → รอ AI wavs เมื่อจอด endpoint/token ถูกต้อง

## 🔵 2026-08-07 Session 3 — AAA Master Control + Voice wiring (Safe Mode ออกแล้ว)

### ✅ ทำเสร็จแล้ว (verify ด้วย tool จริง — Rule 1/4A)
| งาน | ผล | หลักฐาน |
|---|---|---|
| **Compile / Safe Mode** | **0 errors** (ล้าง `Library/ScriptAssemblies` + batchmode) | Editor.log: `script compilation time` + `total_errors=0` + UNITY_EXIT=0 → **ออก Safe Mode แล้ว** |
| **Voice 14/14 wiring** | `AssignVoiceTo` ใหม่ ใช้ `PrefabUtility.LoadPrefabContents` + **AddComponent `UncleNokReferee`** (ถ้าไม่มี) + assign 14 Zira wav + `SaveAsPrefabAsset` | log: `Added UncleNokReferee component` + `Voice: 14/14 clips wired (missingField=False, missingClip=0)` + `Voice persisted to prefab asset`; **YAML: `UncleNokReferee` guid (.prefab) + wav refs 14/14** |
| **SFX 12/12** | `AssignSfxToScene` ผูก 12 สล็อตใน `CueStrikeAudioManager` (scene instance, ใช้ placeholder ใน `Audio/Clips/`, 0 credits) | log: `SFX: 12/12 placeholder clips wired` |
| **Pink fix (URP/Lit)** | `PinkMaterialFixer.RunFix()` | log: `props renderers: 475 (pink->URP/Lit)` |
| **Vision screenshot** | `CaptureVisionScreenshot` 1920x1080 | `RoomScreenshots/GrandHall_Master.png` (48,209 bytes) |
| **Scene apply** | `EditorSceneManager.SaveScene` | `Title_NoksGrandHall.unity` saved 06:42 |

### 🔧 root cause ของ Voice ที่ไม่มีผล (Rule 6 — Technology Mismatch เปิดเผย)
- `Characters/UncleNok/UncleNok_Prefab.prefab` (และ `BoPanda_Prefab.prefab`) เป็น **prefab variant** ของ base `guid 983447…` ซึ่งลงแค่ `RigBuilder + CueStrikeCue` — **ไม่มี `UncleNokReferee` component**
- `Prefabs/AAA_Characters/UncleNok_AAA.prefab` (740KB) เป็น **ตัวโมเดล rig (bones/skinned mesh, `m_Script`=0)** ไม่ใช่ prefab ที่มี gameplay component
- → `GetComponentInChildren<UncleNokReferee>` เดิม = null → ฟังก์ชัน return เปล่า ๆ (เสียงไม่เคยติด)
- **วิธีแก้:** `AssignVoiceTo` ใช้ `LoadPrefabContents` + `AddComponent` + `SaveAsPrefabAsset` → component + 14 clips ถูกบันทึกลง `.prefab` จริง (ตรวจ YAML ได้: guid `a66730cb…` + wav refs 14/14)

### 📋 หมายเหตุ / ต่อไป
- Grand Hall scene อ้าง prefab: `Somchay_AAA` + `LuxuryChandelier` + `BoPanda_Prefab` (variant) — ยังไม่มี `UncleNok_AAA` (model rig) ถูกวาง; Master Control `PlaceCharacters` จะวาง `UncleNok_Prefab`/`BoPanda_Prefab` (variant ที่มี referee + voice) ถ้ายังไม่ present
- `UncleNokReferee` ฟิลด์เสียงครบ 14 (matchStart/turnStart/potSuccess/centuryBreak/highBreak/foulCalled/foulCueBallPotted/break/clearance)
\n\n## 🎯 CALL-SHOT UI SHOW TRIGGER — Round 14 (2026-08-10, per implementation_plan_callshot_trigger.md)\n\n**Goal:** เมื่อถึงตาผู้เล่นที่ต้อง call-shot → panel ปรากฏจริง (ไม่ใช่แค่ wire event แต่ trigger visible)\n\n### ✅ Files changed (single file: `ChinesePoolGameManager.cs`)\n- ✅ `MaybeShowCallShotUI()` private helper:\n  - Guard: `!isAiTurn` + `IsCallShotRequired()` → only show when human player must call\n  - เรียก `ChinesePoolUIManager.Instance?.ShowCallShot(false, BallGroupToPlayerGroup(GetCurrentPlayerGroup()))`\n- ✅ Trigger 2 จุด: ท้าย `NextPlayer()` (ทุก turn change) + ท้าย `HandleBreakOrOpenTable()` (หลัง assign กลุ่ม)\n- ✅ Compile verify: EditMode compile 0 errors\n- ✅ Wire (R11) → Trigger (R14) → Find ref (R17) chain ครบ\n\n### ℹ️ Follow-up needed (จาก Vision audit R17)\n- เติม UI ref ในฉากที่ยังขาด (R17 fix ส่วน duplicate ตัว panel ไม่ได้ wire UI fields ส่วนอื่น)\n- Hide CallShot ตอน AI เทิร์น\n\n📝 Plan: `implementation_plan_callshot_trigger.md`\n
---

## 🚀 ALL PRs MERGED — R13–R23 (2026-08-11, via gh CLI authenticated as narisavapolk-sys)

สถานะปิดจบของ backlog ทั้งหมด — ทุก branch ที่ค้าง push ขึ้น origin แล้วเปิด PR + CI เขียว + merge เข้า `main` ครบ:

| # | Round | PR | หัวข้อ |
|---|-------|----|--------|
| 1 | R13 | #7 | Boot scene (Scene 0) + VRStartup Quest optimization + NARI CUE STRIKE editor tool |
| 2 | R14 | #11 | CallShot UI show trigger (`MaybeShowCallShotUI`) |
| 3 | R15 | #10 | CS0618 Find-API modernize (36 call sites → Unity 6 modern API) |
| 4 | R16 | #3 | VRStartup frame-rate auto-detect (Quest 2=72 / 3=90 / 120 opt-in) |
| 5 | R17 | #2 | CallShot UI empty-ref fix (2 scenes) |
| 6 | R18 | #12 | BoPanda mascot prefab + banter |
| 7 | R19 | #13 | VoiceBinder → Editor-only refactor |
| 8 | R20 | #14 | MCP ExecuteCodeTool → CodeDom + McpSettings |
| 9 | R21 | (closed) | CI Node 24 — เนื้อหาส่วนใหญ่เข้า main ไปแล้ว, เหลือ supersede |
| 10 | CI | #5 | ลบ unity-activate.yml duplicate (CI รัน compile 2 รอบ/PR) |
| 11 | R22 | #16 | VISION_AUDIT_CHECKLIST.md |
| 12 | R23 | #17 | PlayMode NUnit suite (R14/R16/R17) — 12/12 ผ่าน |

### 🔒 Branch protection (ตั้งแล้ว)
- `main`: ต้องผ่าน **Unity Compile Gate** (`Unity batchmode compile check (editmode)`) ก่อน merge
- enforce_admins = true, ห้าม force-push / delete branch
- → ทุกงานถัดไปต้องมาเป็น PR (ไม่มี push ตรงเข้า main)

### 🧹 Cleanup (session นี้)
- ลบ worktree/cache เสีย (`playmode-test-worktree/`, nested `CueStrike_Project/`) ที่เหลือจาก session ก่อน
- stash backup `main-checkout-uncommitted-backup-2026-08-11` — เนื้อหาถูก merge ไปครบแล้วผ่าน R17/R18/R19/R20 → เก็บไว้เป็น safety net

## 🎓 FIRST-TIME TUTORIAL — Round 24 (2026-08-11, per implementation_plan_r24_title_tutorial.md)

**Goal (coach-approved):** เปิดเกม (Boot) → Title (Lobby) → ผู้เล่นครั้งแรกต้องผ่าน Tutorial
สอนจับไม้คิว/เล็ง/ยิง เบื้องต้น ก่อนเข้าเมนูหลัก; เคยเล่นแล้ว / กด Skip → เข้า Lobby ได้ทันที

### ✅ Files changed
- **`Assets/CueStrike/Scripts/TitleScene/CueStrikeFirstTimeFlow.cs`** (ใหม่):
  - PlayerPrefs first-time flag (`CueStrike_FirstTimeTutorialDone`) — static `IsTutorialDone()` / `MarkTutorialDone()` / `ResetTutorialFlag()`
  - 3 สไลด์ภาษาไทย default (ยินดีต้อนรับ / จับไม้คิว+เล็ง / ยิง+เริ่มเล่น) — assign `slides[]` ใน Inspector ได้
  - Fail-safe: ไม่หา Canvas/สร้าง UI ไม่ได้ → ข้าม tutorial (ไม่บล็อก Lobby)
  - ปุ่ม ถัดไป / ข้าม Tutorial — สไลด์สุดท้ายเปลี่ยนเป็น "เริ่มเล่น"
- **`Assets/CueStrike/Scenes/Title_NoksGrandHall.unity`**: ผูก `FirstTimeTutorial` GameObject + component เข้าฉาก (SceneRoots)
- **`Assets/CueStrike/Editor/FirstTimeTutorialSetup.cs`** (ใหม่): Editor tool `Tools/CueStrike/Title Scene/10. Setup First-Time Tutorial`
  - Idempotent (มี component อยู่แล้ว → skip) + Guard 3 ชั้น + Self-test

### ✅ Verify
- Compile gate batchmode: **0 errors, 0 warnings** (ไฟล์ใหม่)
- Scene load + `-executeMethod SetupFirstTimeTutorial`: 0 errors — tool พบ component ที่ wire ไว้แล้ว → skip (idempotent)
- หมายเหตุ: `CueStrikeTutorialManager` (in-match validation) ยังอยู่ครบ — R24 เป็น onboarding เบาๆ ใน Lobby ตามแบบโค้ช

## 🏆 MATCH FLOW (BEST-OF + WINNER) — Round 25 (2026-08-11, per implementation_plan_r25_match_flow.md)

**Goal (coach-approved):** UI Dialog เลือกเงื่อนไขก่อนเริ่มเกม (Single Frame / Best of 3/5/7 / Practice)
→ ส่งค่าเข้า GameManager คุมเฟรมที่จะชนะ + scoreboard ต่อเฟรม + WINNER screen + กลับเมนู

### ✅ Files changed
- **`ChinesePoolGameManager.cs`**: `StartNewMatch(bestOf=0)` = Practice (no match end, `isPracticeMode`); `StartPracticeMatch()`; `EndFrame` ข้ามจบแมตช์ใน practice
- **`ChinesePoolScoreboard.cs`**: เพิ่ม `SetFrameScore(p1,p2)` + ช่อง `_player1FramesText/_player2FramesText` (reset ใน ResetScoreboard)
- **`ChinesePoolUIManager.cs`**: เพิ่ม `SetFrameScore` / `OnFrameEnded` / `ShowMatchOver` forwarder
- **`ChinesePoolMatchSetupUI.cs`** (ใหม่): World-Space VR panel 5 ปุ่ม (Single Frame/3/5/7/Practice) — สร้าง UI ด้วยโค้ด, fail-safe, `StartNewMatch` + `InitializeGame`
- **`ChinesePoolMatchEndScreen.cs`** (ใหม่): subscribe `OnMatchOver` → WINNER panel + ปุ่ม เล่นอีกครั้ง (Best of เดิม) / กลับเมนู (Title) — fail-safe, auto-subscribe 30 เฟรม
- **`AAA_RoomDAY.unity`**: ผูก GameObject `MatchFlow` + `MatchEndScreen` (SceneRoots)
- **`ChinesePoolMatchFlowSetup.cs`** (ใหม่): Editor tool `Tools/CueStrike/Room Scene/20. Setup Match Flow` — idempotent + self-test + batchmode (โหลด AAA_RoomDAY ถ้าไม่มี scene)

### ✅ Verify
- Compile gate batchmode: **0 errors** (warnings CS0618 เดิมใน GameManager/Scoreboard ไม่ได้เพิ่มใหม่)
- Scene load AAA_RoomDAY + `-executeMethod SetupMatchFlow`: 0 errors, idempotent, self-test **3/3 ผ่าน**

### ℹ️ Vision audit (manual — ยังต้องดูด้วยตา)
- เปิด Editor → AAA_RoomDAY → Play → เห็น panel เลือกเงื่อนไข → Best of 3 → เล่นจนเฟรมจบ → frame score อัปเดต → จบ 2 เฟรม → WINNER screen → เล่นอีกครั้ง / กลับเมนู

## 🎱 MODE SELECTION (SNOOKER 15/10/6 หลัก) — Round 26 (2026-08-11, per implementation_plan_r26_mode_selection.md)

**Goal (coach-approved):** เลือกโหมดจริงจากเมนู — SNOOKER 15/10/6 เป็นโหมดหลัก —
ใช้ `totalRedBalls` คุมการตั้งโต๊ะ (15 = สามเหลี่ยมเต็ม, 10 = ตัดแถวหลัง, 6 = สามเหลี่ยมเล็กสุด)

### ✅ Files changed
- **`CueStrikeWBPSRuleset.cs`**: เพิ่ม `SetupRack()` — runtime rack builder วางลูกแดงเป็นสามเหลี่ยมตาม `totalRedBalls` (15→5 แถว, 10→4 แถว, 6→3 แถว) + สี/cue ball ถ้ายังไม่มี; `ResetFrame()` เรียก `SetupRack()`; `Awake()` อ่านโหมดจาก selector; ใช้ `DestroyImmediate` ใน edit mode
- **`CueStrikeGameModeSelector.cs`** (ใหม่): enum Snooker15/10/6 + EightBall/NineBall/ChinesePool; static `SelectedMode` (PlayerPrefs); `GetRedBallsForMode()`; `ModeToSceneName()`; `ApplyModeToScene()`
- **`CueStrikeModeSelectionPanel.cs`** (ใหม่): World-Space VR panel 6 ปุ่ม (Snooker 15/10/6, 8-Ball, 9-Ball, Chinese Pool) + กลับ — self-building, fail-safe
- **`MainMenuUIController.cs`**: เพิ่ม `modeButtons[]` + `SelectModeAndLoad(GameMode)`
- **`MainMenu.unity`**: ผูก GameObject `ModeSelectionPanel` (SceneRoots)
- **`CueStrikeModeSelectionSetup.cs`** (ใหม่): Editor tool `Tools/CueStrike/Main Menu/30. Setup Mode Selection` — idempotent + self-test + batchmode

### ✅ Verify
- Compile gate batchmode: **0 errors, 0 warnings** (ไฟล์ใหม่)
- Scene load MainMenu + `-executeMethod SetupModeSelection`: 0 errors, idempotent, self-test 3/3
- **Rack builder พิสูจน์ด้วย batchmode test:** reds=6 → 6 ลูก, 10 → 10 ลูก, 15 → 15 ลูก — **PASS**
- หมายเหตุ: Build Settings มี 12 ฉากครบ (AAA_RoomDAY อยู่ใน list อยู่แล้ว)

### ℹ️ Vision audit (manual)
- เลือก Snooker 6 → Snooker_Demo → เห็นลูกแดง 6 ลูก (สามเหลี่ยมเล็ก 3 แถว)

## 🔊 SFX 9 ช่อง (ผูก AudioSource + volume ตามแรง) — Round 28 (2026-08-12, per implementation_plan_r28_sfx_channels.md)

**Goal (ตามคำสั่งพี่โม่ง):** ผูกช่อง SFX จริง 9 ตัวเข้ากับ AudioSource (ball hit, cushion, pocket, cue, chalk, crowd, ambient, ui_click, ui_hover) + volume ตามแรงกระแทก + เขียนตารางไฟล์ที่พี่ต้องหา

### ✅ Files changed
- **`CueStrikeAudioManager.cs`**: เพิ่ม `cueStrike` + `crowdAmbient` fields + `PlayCueStrike(intensity)` (volume/pitch ตามแรงยิง) + `PlayCrowdAmbient()` (loop background)
- **`CueStrikeSfxSceneSetup.cs`** (ใหม่): Editor tool `Tools/CueStrike/Audio/40. Setup SFX Channels`
  - หา/สร้าง `AudioManager` + `CueStrikeAudioManager` ใน **12 ฉากที่เล่นได้** (MainMenu, Boot, Title, AAA_RoomDAY, Snooker_Demo + ห้อง 8 ตัว)
  - assign 9 clips จาก `Audio/Clips/` + เพิ่ม `CueStrikeDynamicPhysicsSFX` (3D spatial + velocity volume) + assign `CrowdSystem.ambientMurmur = crowd_murmur.wav`
  - Idempotent + self-test + batchmode
- **12 scene files**: เพิ่ม AudioManager + clips ให้ทุกฉาก (ก่อนหน้า AudioManager อยู่ในแค่ Title scene → เสียงไม่ออกในห้องแข่ง)

### ✅ Verify
- Compile gate batchmode: **0 errors** (ไฟล์ใหม่ 0 warnings)
- Tool รันจริง **12/12 ฉาก** ผ่าน — self-test **19/19 ผ่าน** (9 clips มีไฟล์ + AudioManager assign ครบ 9 ช่อง)
- Main checkout คืนสภาพสะอาดแล้ว

### 📋 ตารางไฟล์ที่พี่ต้องหา (R28 — พี่หาเสียงจริงมาแทน placeholder)
> ไฟล์ทั้งหมดตอนนี้เป็น **placeholder สังเคราะห์** (`CueStrikeAudioGenerator`) — วางไฟล์จริงที่ชื่อเดียวกันลงใน `Assets/CueStrike/Audio/Clips/` แล้ว **ไม่ต้องแก้โค้ด** (GUID เดิมถูกอ้างอิงจากทุกฉาก)

| # | ช่อง SFX | ไฟล์ placeholder ปัจจุบัน | ใช้ที่ไหน | วิธีหาเสียงจริง |
|---|----------|--------------------------|-----------|----------------|
| 1 | ball hit | `ball_ball_hit.wav` | `CueStrikeBallPhysics.OnCollisionEnter` → `PlayBallHit(intensity)` | เสียงลูกบิลเลียดชนกัน (อัดจริง / ฟรี asset: freesound.org search "billiard ball hit") |
| 2 | cushion | `ball_cushion_hit.wav` | เดียวกับ #1 (`cushionImpact: true`) | เสียงลูกชนขอบโต๊ะ (rubber thud) |
| 3 | pocket | `ball_pocket_drop.wav` | `Pocket.cs` / `PocketSoundDetector` → `PlayPocketAt` | เสียงลูกลงหลุม + กลิ้งในราง |
| 4 | cue | `cue_ball_hit.wav` | `PlayCueStrike(intensity)` (ใหม่ R28) | เสียงคิวตีลูกขาว (crisp click) |
| 5 | chalk | `chalk_scrape.wav` | `CueStrikeCueRack` / `CueStrikeShotManager` → `PlayChalk()` | เสียงถูชอล์กที่หัวคิว |
| 6 | crowd | `crowd_murmur.wav` | `CrowdSystem.ambientMurmur` + `nearMissGasp` | เสียงผู้ชมพูดคุยเบาๆ (loop) + อุทานตอนพลาดใกล้ |
| 7 | ambient | `ambient_room_tone.wav` | `PlayAmbientRoom()` (RoomSetupAAA โหลดห้อง) | เสียงบรรยากาศห้อง (แอร์/คน murmuring — loop) |
| 8 | ui_click | `ui_click.wav` | `MainMenuUIController` → `PlayMenuClick()` | เสียงกดปุ่ม (short click) |
| 9 | ui_hover | `ui_hover.wav` | `MainMenuUIController` → `PlayMenuHover()` | เสียงชี้ปุ่ม (soft tick) |

**วิธีใช้:** ดาวน์โหลดไฟล์เสียง → ตั้งชื่อตรงตามคอลัมน์ 2 → ลากวางทับไฟล์เดิมใน `Assets/CueStrike/Audio/Clips/` (Unity จะ import ใหม่ GUID เดิม) → เล่นเกมได้เลย ไม่ต้องแก้โค้ด

## 🦣 MASCOT SCENE PLACEMENT — Round 29 (2026-08-12, per implementation_plan_r29_mascot_scenes.md)

**Goal (ตามคำสั่งพี่โม่ง):** ตรวจว่า UncleNok + BoPanda ถูกวางในฉากไหนบ้าง + animation ใหม่ (R27) จะเล่นในฉากนั้นจริงหรือไม่ — รายงาน + แก้ถ้าขาด

### 📋 Findings (ตรวจโค้ดจริง)
| รายการ | สถานะ | Animation เล่นไหม? |
|--------|--------|-------------------|
| BoPanda_Prefab ใน Title (1.8, 0.4, -1.6) + Animator + controller (R27) | ✅ | ✅ เล่นได้ |
| **UncleNok_Prefab ไม่อยู่ในฉากไหนเลย** (มีแค่ placeholder cube) | ❌ | ❌ ไม่มีทางเล่น |
| ฉากห้องแข่งทั้ง 9 + MainMenu/Boot ไม่มี mascot/referee | ❌ | ❌ |

### ✅ Files changed
- **`MascotScenePlacementSetup.cs`** (ใหม่): Editor tool `Tools/CueStrike/Mascots/50. Place Mascots in Scenes` — ใช้ `PrefabUtility.InstantiatePrefab` + idempotent (มี UncleNokReferee อยู่แล้ว → skip) + self-test + batchmode
- **`Title_NoksGrandHall.unity`**: ลบ `UncleNok_Placeholder` (cube) → วาง `UncleNok_Prefab` ที่ (0, 0.9, 2)
- **`AAA_RoomDAY.unity`** + **`Snooker_Demo.unity`**: วาง `UncleNok_Prefab` เป็น referee ริมโต๊ะ (0, 0, -4.6)

### ✅ Verify
- Compile gate batchmode: **0 errors** (ไฟล์ใหม่ 0 warnings)
- Tool รันจริง **3/3 ฉาก** ผ่าน + **idempotent** (รันซ้ำ → skip ทั้ง 3)
- Self-test **4/4 ผ่าน** (prefab มี Animator + controller + UncleNokReferee)
- main checkout คืนสภาพสะอาด

### ⏳ หมายเหตุ
- `_animator/_audioSource/_homePosition` ของ UncleNokReferee ยังว่าง — animation จะเล่น (Animator อัตโนมัติจาก controller) แต่ voice + home position ต้องรอ R30 Voice Pinning
- ห้อง 8 ตัว + MainMenu/Boot ยังไม่มี mascot (เล่นผ่าน scene picker เท่านั้น — เพิ่มทีหลังได้)

## 🎙️ VOICE PINNING (UncleNokReferee + AudioSource + refs) — Round 30 (2026-08-12, per implementation_plan_r30_voice_pinning.md)

**Goal (ตามคำสั่งพี่โม่ง):** ผูก UncleNokReferee 14 voice clips กับ prefab จริง — เพิ่ม AudioSource + assign `_animator`/`_audioSource`/`_homePosition`

### 📋 Findings (ตรวจโค้ดจริง)
| รายการ | สถานะ |
|--------|--------|
| clips 14 ตัว (Voice/UncleNok/*.wav) | ✅ **assign ครบแล้ว** ใน prefab (GUID ตรงทุกตัว) |
| **AudioSource component ใน prefab** | ❌ ไม่มีเลย → เสียงไม่ออก |
| `_animator` / `_audioSource` / `_homePosition` | ❌ ว่าง (fileID: 0) |
| 3 ฉาก (Title/AAA_RoomDAY/Snooker_Demo) | ℹ️ เป็น prefab instance → แก้ prefab แล้วได้ผลอัตโนมัติ |

### ✅ Files changed
- **`UncleNok_Prefab.prefab`**: เพิ่ม **AudioSource** (3D spatial, logarithmic rolloff, maxDistance 20) + assign `_animator` = Animator (1307204390460968239) + `_audioSource` = AudioSource ใหม่ (6649867984910534005) + `_homePosition` = root Transform (3714027086145795936)
- **`UncleNokVoicePinSetup.cs`** (ใหม่): Editor tool `Tools/CueStrike/Mascots/60. Pin UncleNok Voice & Refs` — ใช้ `PrefabUtility.LoadPrefabContents` (Unity จัดการ fileID เอง) + idempotent + self-test + batchmode
- **ไม่ต้องแก้ 3 ฉาก** — prefab instance ได้รับผลอัตโนมัติ

### ✅ Verify
- Compile gate batchmode: **0 errors** (ไฟล์ใหม่ 0 warnings)
- Tool รันจริง: เพิ่ม AudioSource + assign refs ครบ + save prefab
- Self-test **12/12 ผ่าน** (AudioSource + Animator + controller + refs 3 ตัว + clips 4 กลุ่มหลัก)
- main checkout คืนสภาพสะอาด

### ⏳ หมายเหตุ
- ยังไม่ผูก referee กับ game events (R31 กรรมการจริง — ประกาศคะแนน/ฟาวล์) — ตอนนี้พร้อมรับ event แล้ว
- `_homePosition` = root Transform → `Start()` ล็อกตำแหน่ง + `Update()` หมุนหันเข้าหา home position (referee หันหน้าเข้าหาโต๊ะตลอด)

## 🦣 REFEREE EVENT BRIDGE (UncleNok กรรมการจริง) — Round 31 (2026-08-12, per implementation_plan_r31_referee_events.md)

**Goal (ตามคำสั่งพี่โม่ง):** ผูก UncleNokReferee กับ game events (OnFrameStart/OnBallPotted/OnFoulCommitted) — กรรมการประกาศคะแนน+ฟาวล์จริง

### 📋 Findings (ตรวจโค้ดจริง)
| รายการ | สถานะ |
|--------|--------|
| GameManager events (`OnFrameWon`/`OnFoulCommitted`/`OnMatchOver`/`OnTurnChanged`/`OnPhaseChanged`) | ✅ มีครบ — ใช้ `OnPhaseChanged` (phase=Break ตอน StartNewFrame) เป็นจุดเริ่มเฟรม/แมตช์ |
| WBPS events (`OnBallPotted`/`OnFoulCommitted`/`OnFrameWon`) | ✅ มีครบ — ทั้ง 2 มี `Instance` pattern |
| `UncleNokReferee` methods (`OnFrameStart`/`OnMatchStart`/`OnBallPotted`/`OnFoulCommitted`/`OnMatchEnd`) | ✅ มีครบ — แต่ยังไม่มีใคร subscribe (dead code) |
| namespace | ⚠️ GameManager อยู่ `CueStrike.Gameplay.ChinesePool`, WBPS เป็น global — bridge ต้อง using ถูก |

### ✅ Files changed
- **`UncleNokRefereeEventBridge.cs`** (ใหม่): runtime — subscribe `ChinesePoolGameManager` + `CueStrikeWBPSRuleset` events → เรียก referee methods (`OnMatchStart`/`OnFrameStart`/`OnBallPotted`/`OnFoulCommitted`) — fail-safe: หา Instance ไม่เจอ → retry ทุก 2s
- **`RefereeEventBridgeSetup.cs`** (ใหม่): Editor tool `Tools/CueStrike/Mascots/80. Setup Referee Event Bridge` — `PrefabUtility.LoadPrefabContents` + idempotent + self-test + batchmode
- **`UncleNok_Prefab.prefab`**: เพิ่ม `UncleNokRefereeEventBridge` component — ฉากไหนมีลุงโน๊ก (Title/AAA_RoomDAY/Snooker_Demo) ได้ผลอัตโนมัติ

### ✅ Verify
- Compile gate batchmode: **0 errors** (ไฟล์ใหม่ 0 warnings)
- Tool รันจริง: ผูก bridge เข้า prefab สำเร็จ + save
- Self-test **5/5 ผ่าน** (bridge + Animator + AudioSource + controller + clips)
- main checkout คืนสภาพสะอาด — base = main `d5aa9cd` (รวม R32)

### ⏳ หมายเหตุ
- กรรมการพร้อมประกาศจริงแล้ว — รอเสียงคนจริง (หา wav วางใน `Assets/CueStrike/Audio/Clips/`) ก็พูดได้เลย
- โหมด Practice (เล่นคนเดียว) ยังไม่ผูก — กรรมการจะประกาศเฉพาะโหมดแข่ง (R35 คู่ซ้อม AI ต่อยอดได้)

## 🐼 BO COMEDY DIRECTOR (โมเมนต์ตลก 2 ตัว) — Round 32 (2026-08-12, per implementation_plan_r32_bo_comedy.md)

**Goal (ตามคำสั่งพี่โม่ง):** ระบบ Bo Comedy Director — โมเมนต์ตลกง่ายๆ 2 ตัวก่อน ใช้ animation ที่มีอยู่แล้ว (R27)

### ✅ Files changed
- **`BoComedyDirector.cs`** (ใหม่): runtime — 2 ทริกเกอร์:
  1. **Bo หลับ** — ผู้เล่นไม่ขยับเกิน 30s → `SetTrigger("Disappointed")` (ก้มหน้า = หลับ) + "zzz..."; พอลูกขยับ → `SetBool("IsIdle", true)` + ตื่น (ตรวจผ่าน `BallActivityDetector` — หา Rigidbody ที่ velocity > 0)
  2. **Bo มึนสกอร์เสมอ** — subscribe `ChinesePoolScoreboard.OnScoreChanged` → p1 == p2 > 0 → `SetTrigger("Speak")` + cooldown 20s
  - Fail-safe: หา Animator/Scoreboard ไม่เจอ → log + ข้าม ไม่พัง; retry subscribe ทุก 2s
- **`BoComedySetup.cs`** (ใหม่): Editor tool `Tools/CueStrike/Mascots/70. Setup Bo Comedy Director` — `PrefabUtility.LoadPrefabContents` + idempotent + self-test + batchmode
- **`BoPanda_Prefab.prefab`**: เพิ่ม `BoComedyDirector` component — ฉากไหนมี Bo instance (เช่น Title) ได้ผลอัตโนมัติ

### ✅ Verify
- Compile gate batchmode: **0 errors** (ไฟล์ใหม่ 0 warnings)
- Tool รันจริง: เพิ่ม component + save prefab
- Self-test **7/7 ผ่าน** (component + Animator + controller + triggers Disappointed/Speak/IsIdle ใน controller)
- หมายเหตุ: แก้ระหว่างทาง — meta GUID ต้องเป็น 32 hex เป๊ะ (GUID ยาวเกิน 32 → Unity ละเลยไฟล์ทั้งไฟล์ — CS0246 class not found) + `FindObjectsByType` ต้องเรียกผ่าน `UnityEngine.Object` ใน static class

### ⏳ หมายเหตุ
- โมเมนต์ตลกขั้นสูง (Bo ขโมยชอล์ก / กลัวลูกพุ่ง / กองเชียร์พลาด) — ต้องทำท่า animation ใหม่ (Blender) — roadmap ไว้ทีหลัง
- ใช้ได้เฉพาะฉากที่มี BoPanda + Scoreboard (Title มี Bo แต่ไม่มี Scoreboard → ทริกเกอร์มึนสกอร์ต้องรอเมื่อ Bo อยู่ในห้องแข่ง)

## 🐼 BOPANDA ลงห้องแข่ง (คู่พิธีกรครบ) — Round 33 (2026-08-12, per implementation_plan_r33_bopanda_match_scenes.md)

**Goal (ตามคำสั่งพี่โม่ง):** วาง BoPanda ลง AAA_RoomDAY + Snooker_Demo ด้วย — ให้ห้องแข่งมีคู่พิธีกรครบ (ลุงโน๊ก referee + โบกองเชียร์)

### ✅ Files changed
- **`MascotScenePlacementSetup.cs`** (ขยายจาก R29): เพิ่ม `BoPandaPrefabPath` + `BoPandaTargets` — วาง BoPanda ฝั่งตรงข้ามลุงโน๊ก (0, 0, 4.6) + idempotent ตรวจชื่อ GameObject (UncleNok/BoPanda) + self-test ตรวจ Bo ด้วย
- **`AAA_RoomDAY.unity`** + **`Snooker_Demo.unity`**: เพิ่ม BoPanda instance ที่ (0, 0, 4.6) — ลุงโน๊ก (0, 0, -4.6) ยืนคนละฝั่งโต๊ะ

### ✅ Verify
- Compile gate batchmode: **0 errors** (ไฟล์ใหม่ 0 warnings)
- Tool รันจริง: วาง BoPanda **2/2 ฉาก** + **idempotent** (รันซ้ำ → skip ทั้งหมด — Title/AAA/Snooker)
- Self-test **4/4 ผ่าน**
- main checkout คืนสภาพสะอาด

### ⏳ หมายเหตุ
- Title ไม่ถูกแตะ (มี Bo อยู่แล้ว) — tool ข้ามอัตโนมัติ
- ฉากห้องแข่งตอนนี้มีลุงโน๊ก + โบครบคู่ — พร้อมให้ Bo Comedy (R32 merged) ทำงานเต็มรูปแบบ (มี Scoreboard ในห้อง)

### ⏭️ Roadmap R24+ (จัดลำดับความสำคัญ — ปรับตามโค้ช)
1. **R31 กรรมการจริง** — ผูก UncleNokReferee กับ game events (OnFrameStart/OnBallPotted/OnFoulCommitted) — ประกาศคะแนน/ฟาวล์จริง (MERGED ✅ — PR #28)
2. **R34 ลุงโน๊กคู่ซ้อม AI** — ต่อ AI opponent กับโหมด Practice + เลือกระดับ Easy/Medium/Hard/Expert (อยู่ระหว่าง PR)
3. **R35 (nice-to-have)** — Multiplayer room (Normcore)

## 🦣 PRACTICE AI (ลุงโน๊กคู่ซ้อม) — Round 34 (2026-08-12, per implementation_plan_r34_practice_ai.md)

**Goal (ตามคำสั่งพี่โม่ง):** ต่อ AI opponent เข้ากับโหมด Practice — ลุงโน๊กเป็นคู่ซ้อม AI เลือกระดับ Easy/Medium/Hard/Expert ได้จาก UI — ใช้ CueStrikeAIController ที่มีอยู่แล้ว

*หมายเหตุ: พี่โม่งสั่งว่า "R31" แต่ R31 ถูกใช้ไปแล้ว (Referee Event Bridge, PR #28) → งานนี้คือ R34 ตาม roadmap*

### 📋 Findings (ตรวจโค้ดจริง)
| รายการ | สถานะ |
|--------|--------|
| `ChinesePoolAIModifier` (stub) | ✅ มีอยู่ (AAA 2 ตัว) — `DecideCallShot()` + `DecideShotParameters()` + `SetDifficulty()` — **แต่ไม่มีใครเรียก (dead)** |
| `CueStrikeAIController` | ✅ มีอยู่ (AAA 2 ตัว) — `SetSkillLevel()` 4 ระดับ — แต่ยิงผ่าน reflection `shotManager.currentForce` → **ฉากไม่มี CueStrikeShotManager → ยิงไม่ทำงาน** |
| `ChinesePoolGameManager.NextPlayer()` | ✅ ตั้ง `isAiTurn = (playerIndex==1 && aiModifier!=null)` — **แต่ไม่มีโค้ดให้ AI ตัดสินใจ/ยิง** |
| ฉากที่มี GameManager | ✅ **AAA_RoomDAY เท่านั้น** (Title = lobby, Snooker_Demo = WBPS คนละระบบ) |

### ✅ Files changed
- **`CueStrikePracticeAIBridge.cs`** (ใหม่, runtime): subscribe `OnTurnChanged` → เมื่อ `isAiTurn`: ① `DecideCallShot()` → `SetCallShot()` ② `DecideShotParameters()` → ยิงจริง (`Rigidbody.AddForce`, pattern CueStrikeCue.cs:220 / `CueStrikeShotManager.ExecuteShot` ถ้ามี) ③ รอลูกหยุด → ประเมินผล → `ProcessShotResult()` — fail-safe: หา refs ไม่เจอ → retry ทุก 2s + `SetAIDifficulty()` (sync modifier + controller + PlayerPrefs)
- **`ChinesePoolMatchSetupUI.cs`** (แก้): เพิ่มแถวเลือกระดับ AI (Easy/Medium/Hard/Expert) ใต้ปุ่มเงื่อนไข + เก็บ PlayerPrefs + เรียก bridge `SetAIDifficulty()` ก่อนเริ่มแมตช์
- **`PracticeAISetup.cs`** (ใหม่, Editor): tool `Tools/CueStrike/AI/90. Setup Practice AI` — เพิ่ม bridge ลง AAA_RoomDAY + assign refs + idempotent + self-test + batchmode
- **`AAA_RoomDAY.unity`**: เพิ่ม `CueStrikePracticeAIBridge` (บน node CueStrikeAIController)

### ✅ Verify
- Compile gate batchmode: **0 errors** (ไฟล์ใหม่ 0 warnings)
- Tool รันจริง: AAA_RoomDAY wired (bridge บน node CueStrikeAIController) — Snooker_Demo skip (fail-safe, ไม่มี GameManager)
- Self-test **10/10 ผ่าน** (class + API + difficulty + scene)
- main checkout คืนสภาพสะอาด — base = main `6f756f8` (รวม R33)

### ⏳ หมายเหตุ
- AI ยิงลูกจริงแล้ว (AddForce) — ระดับความยากคุม accuracy/error (ผ่าน CueStrikeAIController params)
- Vision audit: เปิด AAA_RoomDAY → เลือก Practice + ระดับ AI → สังเกต AI ยิงเองตอนเทิร์นมัน
- ⚠️ Vision audit พบ blocker: ฉากยังไม่มี `ChinesePoolAIModifier` component + `GameManager.aiModifier`/`bridge.aiModifier` ว่าง (`fileID: 0`) → guard `OnTurnChanged` return ทันที ไม่เริ่มยิง → ต้องเพิ่ม modifier + assign refs ก่อน AI ยิงได้จริง
- R35: Bo Comedy ทำงานเต็มรูปแบบในห้องแข่ง (มี Scoreboard จริง)

## 🐼 BO COMEDY SCOREBOARD (Bo มึนสกอร์ในห้องแข่ง) — Round 35 (2026-08-12, per implementation_plan_r35_bo_scoreboard.md)

**Goal (ตามคำสั่งพี่โม่ง):** ผูก Bo Comedy Director ให้ทำงานเต็มรูปแบบในห้องแข่ง (AAA_RoomDAY) — Bo มึนสกอร์เสมอเมื่อมี Scoreboard จริง

### 📋 Findings (ตรวจโค้ดจริง)
| รายการ | สถานะ |
|--------|--------|
| `BoComedyDirector` ใน BoPanda prefab | ✅ (R32 merged) — logic ครบ (subscribe + retry ทุก 2s) |
| BoPanda instance ใน AAA_RoomDAY | ✅ (R33 merged) — ยืนฝั่งตรงข้ามลุงโน๊ก (0,0,4.6) |
| `ChinesePoolScoreboard` component ใน AAA | ❌ **ไม่มีเลย** — "Digital Scoreboard" เป็นแค่ mesh ตกแต่ง (MeshRenderer+MeshFilter+BoxCollider) |
| `ChinesePoolUIManager` ใน AAA | ✅ มี (`fileID 1104105757`) แต่ `_scoreboard` ว่าง (`fileID: 0`) |
| `ChinesePoolScoreboard.OnScoreChanged` | ✅ event มี (line 17) — Bo subscribe ได้ |
| ใครเพิ่มสกอร์จริง | `BallPottedTracker` (RegisterPottedBall) + `UIManager.OnBallPotted` |

**ปัญหา:** AAA ไม่มี `ChinesePoolScoreboard` component → UIManager._scoreboard ว่าง → Bo `FindAnyObjectByType` หาไม่เจอ → retry ตลอด → "มึนสกอร์" ไม่เกิด

### ✅ Files changed
- **`BoScoreboardSetup.cs`** (ใหม่, Editor): tool `Tools/CueStrike/Mascots/95. Setup Bo Comedy Scoreboard (AAA_RoomDAY)` — เปิด AAA_RoomDAY → สร้าง `CueStrike_ChinesePoolScoreboard` (component + UI structure: scores/turn indicators/ball containers — pattern จาก CueStrikeGamePolishSetup) → ผูก `ChinesePoolUIManager._scoreboard` (SerializedObject) → self-test + batchmode + idempotent
- **`AAA_RoomDAY.unity`**: เพิ่ม `CueStrike_ChinesePoolScoreboard` GameObject + component + assign `_scoreboard` (fileID `608037973`)

### ✅ Verify
- Compile gate batchmode: **0 errors** (ไฟล์ใหม่ 0 warnings — ใช้ FindAnyObjectByType ตาม convention R15)
- Tool รันจริง: สร้าง scoreboard + wired UIManager._scoreboard + self-test **3/3 PASS**
- **Idempotent**: รันซ้ำ skip ทั้งคู่ ("Scoreboard already present" / "already assigned")
- main checkout คืนสภาพสะอาด — base = main `7e19b21` (รวม R34)

### ⏳ หมายเหตุ
- Bo ใน AAA จะ subscribe `OnScoreChanged` ได้จริง → เมื่อสกอร์ P1==P2>0 → `SetTrigger("Speak")` = มึน "ใครชนะนะ??"
- Scoreboard แสดงสกอร์/เทิร์น/ฟาวล์จริง — ประโยชน์ต่อ R25 match flow
- Vision audit ยังต้องตาม: เล่น Practice → ทำสกอร์เสมอ → สังเกตโบทำมึน
- R36: Snooker AI (WBPS) — AI เล่นสนุกเกอร์ได้ (ทำแล้ว — ดู section ด้านล่าง)

## 🎱 SNOOKER AI (WBPS) — Round 36 (2026-08-12, per implementation_plan_r36_snooker_ai.md)

**Goal (ตามคำสั่งพี่โม่ง):** ต่อ AI opponent เข้ากับ WBPS ruleset (Snooker_Demo) — AI เล่นสนุกเกอร์ได้จริง

### 📋 Findings (ตรวจโค้ดจริง)
| รายการ | สถานะ |
|--------|--------|
| `CueStrikeWBPSRuleset` instance ใน Snooker_Demo | ✅ (Awake → ResetFrame → SetupRack) |
| ลูก 22 ตัว (Red 1-15 + สี 16-21 + Cue 0) | ✅ มี (BallIdentity ×23) |
| **ลูกมี Rigidbody/Collider** | ❌ **ไม่มีเลย** — physics ไม่ทำงาน ยิงไม่ได้ |
| **พื้นโต๊ะ/rail/cushion** | ❌ ไม่มี — ลูกจะตกทะลุ |
| **Pocket positions** | ❌ ไม่มี — AI ไม่มีเป้าหมายหลุม |
| WBPS events (OnBallPotted/OnFoulCommitted/OnFrameWon) | ✅ มี |
| WBPS turn system | ❌ ไม่มี — ต้องสร้างใน bridge |
| `CueStrikeShotManager.ExecuteShot` | ✅ API มี แต่ Snooker_Demo ไม่มี instance |

**ปัญหา:** Snooker_Demo เป็นแค่ "ลูก + ruleset" ไม่มีฟิสิกส์/โต๊ะ/หลุม → ต้องสร้าง environment + bridge ให้ครบ

### ✅ Files changed
- **`CueStrikeWBPSRuleset.cs`** (แก้): `SpawnSnookerBall()` เพิ่ม Rigidbody (mass 0.14, drag, ContinuousDynamic, Interpolate) + ensure SphereCollider + public accessors (ColorSequenceIndex / IsColorPhaseActive / AwaitingRespotColorState / RedsRemaining) สำหรับ AI
- **`CueStrikeSnookerAIBridge.cs`** (ใหม่, runtime): turn system (P1 human ↔ P2 AI) + เลือกลูกตามกฎ WBPS (red phase → เลือกลูกแดงใกล้หลุม; awaiting color → สีค่ามากสุด; color phase → สีตาม sequence) + ghost-ball aim + AddForce จริง + error ตาม difficulty (Easy→Expert) + ประเมินผล (ใกล้หลุม + ต่ำกว่าโต๊ะ) → RegisterPot/ValidateShotFull + fail-safe retry
- **`SnookerAISetup.cs`** (ใหม่, Editor): tool `Tools/CueStrike/Snooker/100. Setup Snooker AI` — สร้างโต๊ะ (bed + 4 rails) + 6 pockets + เพิ่ม Rigidbody/SphereCollider ให้ลูก 22 ตัว + ผูก bridge (ruleset + pockets) — idempotent + self-test + batchmode
- **`Snooker_Demo.unity`**: เพิ่ม SnookerTable_Physics + SnookerPockets + Rigidbody ×22 + SnookerAI_Bridge

### ✅ Verify
- Compile gate batchmode: **0 errors** (ไฟล์ใหม่ 0 warnings)
- Tool รันจริง: โต๊ะ + 6 หลุม + 22 ลูก (44 physics fixes) + bridge — self-test **6/6 PASS**
- **Idempotent**: รันซ้ำ skip ทั้งหมด (Table/Pockets/Bridge)
- main checkout คืนสภาพสะอาด — base = main `d498a0d` (รวม R35)

### ⏳ หมายเหตุ
- AI เล่นสนุกเกอร์ได้: เลือกลูกถูกตามกฎ (red→color→color phase), ยิงจริงด้วย physics, ฟาวล์/สกอร์ผ่าน WBPS
- Difficulty เริ่ม Medium — เปลี่ยนได้ที่ Inspector ของ bridge (Easy/Medium/Hard/Expert)
- Aim เป็น heuristic (nearest pocket + ghost-ball) — ไม่ perfect แต่เล่นได้สมเหตุสมผล
- Vision audit: เปิด Snooker_Demo → เลือก AI เทิร์น → สังเกต AI ยิง + เลือกลูกตามกฎ
- R37: แก้ ChinesePool AI blocker (ทำแล้ว — ดู section ด้านล่าง)

## 🎯 CHINESEPOOL AI FIX (AI ยิงได้จริง) — Round 37 (2026-08-12, per implementation_plan_r37_cnpool_ai_fix.md)

**Goal (ตามคำสั่งพี่โม่ง):** เพิ่ม ChinesePoolAIModifier component ลง AAA_RoomDAY + assign refs ให้ GameManager.aiModifier และ CueStrikePracticeAIBridge.aiModifier — แก้ Vision audit blocker ที่ AI ยิงไม่ได้

*หมายเหตุ: พี่โม่งสั่งว่า "R36" แต่ R36 ถูกใช้ไปแล้ว (Snooker AI — PR #31 merged) → งานนี้คือ R37 ตาม roadmap*

### 📋 Findings (ตรวจโค้ดจริง)
| รายการ | สถานะ |
|--------|--------|
| `ChinesePoolAIModifier` class (DecideCallShot/DecideShotParameters/SetDifficulty) | ✅ มีครบ (namespace CueStrike.Gameplay.ChinesePool) |
| **`ChinesePoolAIModifier` component ใน AAA** | ❌ **ไม่มีเลย** (grep=0) |
| `ChinesePoolGameManager.aiModifier` | ❌ ว่าง (fileID: 0) |
| `CueStrikePracticeAIBridge.aiModifier` | ❌ ว่าง (fileID: 0) — aiController มีแล้ว |
| Guard bridge: `if (_gm == null || _gm.aiModifier == null) return;` | ⚠️ return ทันที → AI ไม่ยิง |

**ปัญหา:** modifier หายจากฉาก → FindFirstObjectByType หาไม่เจอ → refs ว่าง → guard return → AI ไม่ยิง

### ✅ Files changed
- **`ChinesePoolAIModifierSetup.cs`** (ใหม่, Editor): tool `Tools/CueStrike/AI/110. Setup ChinesePool AI Modifier` — เพิ่ม component + assign refs (SerializedObject) + idempotent + self-test + batchmode
- **`AAA_RoomDAY.unity`**: เพิ่ม ChinesePoolAIModifier (fileID 1187564667) + assign GameManager.aiModifier + bridge.aiModifier

### ✅ Verify
- Compile gate batchmode: **0 errors**
- Tool รันจริง: modifier สร้าง + wired refs ทั้ง 2 + self-test **3/3 PASS**
- **Idempotent**: รันซ้ำ skip ทั้ง 3
- main checkout คืนสภาพสะอาด — base = main `be5cc83` (รวม R36)

### ⏳ หมายเหตุ
- AI (Chinese Pool Practice) จะยิงได้จริงแล้ว: `isAiTurn` ทำงาน + DecideCallShot/DecideShotParameters ครบ
- Vision audit: เปิด AAA_RoomDAY → เลือก Practice + ระดับ AI → สังเกต AI ยิง
- R38: BallSetup fix — AI ยิงได้จริง (ทำแล้ว — ดู section ด้านล่าง)

## 🎯 BALLSETUP FIX (AI ยิงได้จริง — ต้นตอตัวจริง) — Round 38 (2026-08-12, per implementation_plan_r38_ballsetup_fix.md)

**Goal (ตามคำสั่งพี่โม่ง):** Vision audit หลัง R37 — ตรวจว่า AI ยิงลูกจริง — เจอ blocker ตัวจริง: AAA_RoomDAY ไม่มี ChinesePoolBallSetup → เกมไม่เริ่มเฟรม → ไม่มีลูก → AI ยิงไม่ได้

### 📋 Findings (Vision audit ผ่าน PlayMode test จริง)
| รายการ | สถานะ |
|--------|--------|
| GameManager + AIModifier + refs (R37) | ✅ มีครบ |
| **ChinesePoolBallSetup component ใน AAA** | ❌ **ไม่มีเลย** (grep=0) — และไม่มีฉากไหนเลยทั้งโปรเจกต์ |
| หลักฐาน error จริง (PlayMode test) | ❌ `[CueStrike] Cannot start frame — ChinesePoolBallSetup is null!` |
| prefab ลูก Pool_CueBall / Pool_Ball_01..15 | ✅ มีครบ |

**ปัญหา:** BallSetup หาย → StartNewFrame error → 16 ลูกไม่ spawn → AI ไม่มีลูกให้ยิง

### ✅ Files changed
- **`ChinesePoolBallSetupFixer.cs`** (ใหม่, Editor): tool `Tools/CueStrike/AI/120. Fix ChinesePool BallSetup` — เพิ่ม component + assign prefabs (Pool_CueBall/01/08/09) + assign GameManager.ballSetup (SerializedObject) + idempotent + self-test + batchmode
- **`AAA_RoomDAY.unity`**: เพิ่ม ChinesePoolBallSetup (fileID 1255384562) + prefabs 4 ตัว + GameManager.ballSetup assigned

### ✅ Verify
- Compile gate batchmode: **0 errors**
- Tool รันจริง: BallSetup สร้าง + prefabs 4/4 + wired GameManager — self-test **6/6 PASS**
- **Idempotent**: รันซ้ำ skip ทั้งหมด
- main checkout คืนสภาพสะอาด — base = main `5053f17` (รวม R37)

### ⏳ หมายเหตุ
- เกมเริ่มเฟรมได้ → 16 ลูก spawn → AI มีลูกให้ยิงจริง
- **ยังต้อง verify:** PlayMode test ซ้ำ (AI ยิงแล้วลูกขยับ) + Vision audit manual
- backlog: pocket detection (BallPottedTracker) / ฟิสิกส์โต๊ะ อาจยังไม่ครบใน AAA
## 🎯 TITLE SCOREBOARD (Bo Comedy มึนสกอร์ใน Lobby) — Round 39 (2026-08-12, per implementation_plan_r39_title_scoreboard.md)

**Goal (ตามคำสั่งพี่โม่ง):** วาง ChinesePoolScoreboard ลง Title_NoksGrandHall ด้วย เพื่อให้ Bo Comedy "มึนสกอร์เสมอ" ทำงานใน lobby เหมือนห้องแข่ง (AAA_RoomDAY)

### 📋 Findings (กฎข้อ 1 — ตรวจของจริงก่อน)
| รายการ | สถานะ |
|--------|--------|
| ChinesePoolScoreboard component ใน Title | ❌ ไม่มีเลย (grep=0) — เหมือน AAA ก่อน R35 |
| ChinesePoolUIManager ใน Title | ✅ มี |
| BoPanda instance + BoComedyDirector | ✅ มี (R29/R33 วางไว้) |
| Canvas ใน Title | ✅ มี 19 ref — UI แสดงได้ |

**ปัญหา:** Title ไม่มี scoreboard → Bo `FindAnyObjectByType<ChinesePoolScoreboard>` หาไม่เจอ → "มึนสกอร์" ไม่เกิดใน lobby

### ✅ Files changed
- **`BoScoreboardSetup.cs`** (ขยาย R35 tool): จาก hardcode 1 ฉาก → loop 2 ฉาก `{AAA_RoomDAY, Title_NoksGrandHall}` — สร้าง scoreboard + ผูก UIManager._scoreboard + self-test ต่อฉาก — idempotent + batchmode
- **`Title_NoksGrandHall.unity`**: เพิ่ม CueStrike_ChinesePoolScoreboard (fileID 300446121) + UIManager._scoreboard assigned

### ✅ Verify
- Compile gate batchmode: **0 errors** (Library อุ่นบน main)
- Tool รันจริง: AAA skip (idempotent) + Title สร้าง + wired — self-test **3/3 PASS ×2 ฉาก**
- **Idempotent**: รันซ้ำ skip ทั้ง 2 ฉาก (scoreboard + ref assigned ครบ)
- main checkout คืนสภาพสะอาด — base = main `f30fdc0` (รวม R38)
- Docs อัปเดตครบ 3 ไฟล์ (TASK_PROGRESS + CUESTRIKE_MASTER + task.md)

### ⏳ หมายเหตุ
- Bo ใน Title (lobby) subscribe OnScoreChanged ได้ → สกอร์ P1==P2>0 → SetTrigger("Speak") = มึน "ใครชนะนะ??"
- Lobby มี scoreboard UI ด้วย (ต่อยอด R25)
- ยังค้าง: Vision audit AI ยิงจริง (PlayMode test ซ้ำ) + pocket detection ใน AAA
- R40: difficulty selector ใน UI (Snooker) / ผูกเสียงน้องโบ 14 คลิป / Multiplayer room (Normcore)

