# CueStrike VR Billiards — MASTER DOCUMENT
> **Project:** CueStrike VR Billiards (AAA Unity, Meta Quest 2/3)
> **Last Updated:** 2026-08-12
> **Coach:** Strategist/Director | **Dev Agent:** (AI Assistant) | **User:** โม่ง (Mong)
> **Status:** P8 = 100% | P9 = 100% (IK Assist + Shader Fix Complete) | 🧹 House Cleaning R2 done (2026-08-06): 6 junk targets removed + 7 ghost-file refs fixed | ✅ **Compile = 0 Errors REAL (2026-08-06): MCP migrated System.Text.Json → Newtonsoft (UPM) + Rule 6 added** | 🔧 **VCS Setup R3 (2026-08-09): git init + .gitignore + Git LFS — baseline commit `8f7b347`** | 🎬 **Scene Loading Fix R4 (2026-08-09): 11 scenes ใน Build Settings + Practice "hub"→`Snooker_Demo`** | 🧹 **Duplicate Cleanup R5 (2026-08-09): ลบ 4 ไฟล์ที่ไม่มี ref (XR Hands stub, CrowdSystem-Chars, BallSync/GameSync-Normcore) — เก็บ 2 คู่ที่ใช้จริง** | 🚦 **Compile Gate R6 (2026-08-09): `tools/compile_check.sh` + pre-commit hook อัตโนมัติ** | 🎯 **CallShotUI Merge R7 (2026-08-09): 2 เวอร์ชัน → 1 (`CueStrike.UI.ChinesePool`) — GameManager หาเจอจริง** | 🧼 **Scene Name Cleanup R8 (2026-08-09): TitleSceneManager defaults ชี้ฉากจริง/ว่าง** | 🚀 **Remote + Push R9 (2026-08-09): `github.com/narisavapolk-sys/CueStrike` — main พร้อม LFS, PR workflow เริ่มได้** | 🎮 **VR Startup Cleanup R10 (2026-08-09): เก็บ `VR/VRStartup.cs` (ตัวจริง), ลบ `CueStrikeVRStartup.cs` (dead, scene names พัง)** | 🔗 **Call-Shot Wiring R11 (2026-08-09): `OnShotCalled`→`SetCallShot`, `OnCallShotCancelled`→`ClearCallShot`** | 🚦 **GitHub Actions CI R12 (2026-08-09): compile gate ทุก PR (`compile-gate.yml`) + helper `unity-activate.yml`** | 🧹 **CS0618 Find-API Modernize R15 (2026-08-10): 36 deprecated call sites → Unity 6 modern (FindFirstObjectByType / FindObjectsByType) — runtime + editor (13 runtime files + 8 editor files, compile 0 errors)** | 🎯 **VRStartup Frame Rate Auto-Detect R16 (2026-08-10): real Quest device detection (`SystemInfo.deviceModel`) — Quest 2=72Hz, Quest 3=90Hz, optional 120Hz opt-in. OnDestroy guard fix via instance ref. Compile 0 errors.**  🚀 **ALL R13–R23 MERGED (2026-08-11): Boot scene, CallShot trigger, CS0618 modernize, VR frame rate, scene refs, BoPanda, VoiceBinder refactor, MCP CodeDom, PlayMode tests — CI green every PR** | 🔒 **Branch protection ON: main requires Unity Compile Gate + PR** | 🎓 **R24 (2026-08-11): First-Time Tutorial in Title Lobby — `CueStrikeFirstTimeFlow.cs` (PlayerPrefs first-time flag + Skip + fail-safe auto-UI) ผูกเข้าฉาก Title + Editor tool `FirstTimeTutorialSetup` (idempotent + self-test) — compile 0 errors** | 🏆 **R25 (2026-08-11): Match Flow — UI เลือก Single Frame/Best of 3/5/7/Practice + Scoreboard frames + WINNER screen (Rematch/กลับเมนู) — `ChinesePoolMatchSetupUI` + `ChinesePoolMatchEndScreen` ผูก AAA_RoomDAY + practice mode ใน GameManager — compile 0 errors, self-test 3/3** | 🎱 **R26 (2026-08-11): Mode Selection — Snooker 15/10/6 เป็นโหมดหลัก — `CueStrikeGameModeSelector` (static, PlayerPrefs) + runtime rack builder `SetupRack()` ใน WBPS (15→5 แถว/10→4/6→3, พิสูจน์ด้วย batchmode test 6/10/15 ลูก PASS) + `CueStrikeModeSelectionPanel` ผูก MainMenu — compile 0 errors** | 🎬 **R27 (2026-08-11): Character Animation — 4 clips (Idle loop/Celebrate/Disappointed/Speak) ผ่าน Blender pipeline (`create_character_animations_aaa.py`, pose DEF bones โดยตรง + prune 706→28-33 bones, FBX 180-210KB) → remap path เป็น `Somchay_Rig/` → `.anim` 4 ตัว → `UncleNok.controller` states+AnyState transitions → assign ให้ UncleNok+BoPanda prefab + sync referee triggers (Announce→Speak, Disapprove→Disappointed, Thinking→Speak) — compile 0 errors, self-test 5/5** | 🔊 **R28 (2026-08-12): SFX 9 ช่อง — ผูก AudioManager เข้าทุก 12 ฉากที่เล่นได้ (ก่อนหน้าอยู่แค่ Title → เสียงไม่ออกในห้องแข่ง) + assign 9 clips (ball hit/cushion/pocket/cue/chalk/crowd/ambient/ui_click/ui_hover) + `PlayCueStrike(intensity)` volume ตามแรง + `PlayCrowdAmbient()` + Editor tool `CueStrikeSfxSceneSetup` (idempotent + self-test 19/19) + ตารางไฟล์เสียงที่พี่ต้องหาใน TASK_PROGRESS — compile 0 errors** | 🦣 **R29 (2026-08-12): Mascot Scene Placement — ตรวจพบ `UncleNok_Prefab` ไม่ถูกวางในฉากไหนเลย (มีแค่ placeholder cube) → Editor tool `MascotScenePlacementSetup` วาง UncleNok ลง Title (แทน placeholder, 0,0.9,2) + ห้องแข่ง AAA_RoomDAY/Snooker_Demo (ริมโต๊ะ 0,0,-4.6) — BoPanda อยู่ใน Title อยู่แล้ว (animation R27 เล่นได้) — idempotent + self-test 4/4, compile 0 errors** | 🎙️ **R30 (2026-08-12): Voice Pinning — เพิ่ม AudioSource (3D spatial) + assign `_animator`/`_audioSource`/`_homePosition` ใน UncleNok_Prefab (clips 14 ตัว assign ครบอยู่แล้ว) — Editor tool `UncleNokVoicePinSetup` (PrefabUtility.LoadPrefabContents + idempotent + self-test 12/12) — 3 ฉาก (Title/AAA_RoomDAY/Snooker_Demo) เป็น prefab instance ได้ผลอัตโนมัติ — compile 0 errors** | 🐼 **R32 (2026-08-12): Bo Comedy Director — `BoComedyDirector.cs` (Bo หลับเมื่อคิดนานเกิน 30s — ใช้ Disappointed + ตื่นเมื่อลูกขยับ; Bo มึนสกอร์เสมอ — subscribe Scoreboard.OnScoreChanged + Speak + cooldown 20s) + Editor tool `BoComedySetup` ผูกเข้าบ BoPanda_Prefab (idempotent + self-test 7/7) — ใช้ animation R27 ที่มีอยู่แล้ว — compile 0 errors** | 🦣 **R31 (2026-08-12): Referee Event Bridge — `UncleNokRefereeEventBridge.cs` subscribe GameManager+WBPS events (OnMatchStart/OnFrameStart/OnBallPotted/OnFoulCommitted) → กรรมการประกาศคะแนน+ฟาวล์จริง (R30 เตรียม voice ไว้แล้ว) + Editor tool `RefereeEventBridgeSetup` ผูกเข้าบ UncleNok_Prefab (idempotent + self-test 5/5) — compile 0 errors** | 🐼 **R33 (2026-08-12): BoPanda ลงห้องแข่ง — ขยาย `MascotScenePlacementSetup` (R29) วาง BoPanda ฝั่งตรงข้ามลุงโน๊ก (0,0,4.6) ใน AAA_RoomDAY + Snooker_Demo — คู่พิธีกรครบ (ลุง referee + โบกองเชียร์), idempotent + self-test 4/4, compile 0 errors** | 🦣 **R34 (2026-08-12): Practice AI (ลุงโน๊กคู่ซ้อม) — `CueStrikePracticeAIBridge.cs` ผูก AI กับ turn flow (OnTurnChanged → isAiTurn → DecideCallShot → ยิงจริง AddForce → ProcessShotResult) + `ChinesePoolMatchSetupUI` เพิ่มแถวเลือก Easy/Medium/Hard/Expert + Editor tool `PracticeAISetup` ผูก bridge ลง AAA_RoomDAY (ฉากเดียวที่มี GameManager) — self-test 10/10, compile 0 errors** | 🐼 **R35 (2026-08-12): Bo Comedy Scoreboard ในห้องแข่ง — พบว่า AAA_RoomDAY ไม่มี `ChinesePoolScoreboard` component จริง ("Digital Scoreboard" เป็นแค่ mesh ตกแต่ง) + `UIManager._scoreboard` ว่าง → Bo หา scoreboard ไม่เจอ (มึนสกอร์ไม่เกิด) → Editor tool `BoScoreboardSetup` สร้าง scoreboard + UI structure + ผูก `_scoreboard` (SerializedObject, idempotent + self-test 3/3 + batchmode) — Bo จะ subscribe OnScoreChanged ได้จริง ตอนสกอร์ P1==P2>0 → Speak "ใครชนะนะ??" — compile 0 errors** | 🎱 **R36 (2026-08-12): Snooker AI (WBPS) — AI เล่นสนุกเกอร์ได้จริง: พบว่า Snooker_Demo มีแค่ลูก+ruleset (ไม่มี Rigidbody/Collider/โต๊ะ/หลุม → physics ตาย) → แก้ `CueStrikeWBPSRuleset` (Rigidbody+accessors) + `CueStrikeSnookerAIBridge` (turn system P1↔P2, เลือกลูกตามกฎ red→color→color phase, ghost-ball aim, AddForce + difficulty error, ประเมินผล→RegisterPot) + Editor tool `SnookerAISetup` (สร้างโต๊ะ+6 หลุม+physics 22 ลูก+ผูก bridge, idempotent + self-test 6/6) — compile 0 errors** | 🎯 **R37 (2026-08-12): ChinesePool AI Fix — แก้ Vision audit blocker: AAA_RoomDAY ไม่มี `ChinesePoolAIModifier` component + `GameManager.aiModifier`/`bridge.aiModifier` ว่าง → guard ใน OnTurnChanged return ทันที (AI ไม่ยิง) → Editor tool `ChinesePoolAIModifierSetup` เพิ่ม component + assign refs ทั้ง 2 (SerializedObject, idempotent + self-test 3/3 + batchmode) — AI Practice ยิงได้จริง — compile 0 errors** | Ready for Next Phase

> ## ⚠️ MANDATORY: อ่านก่อนทำงานทุกครั้ง
> **AI ทุกตัวต้องอ่าน [`AI_TOOLS_MANDATE.md`](AI_TOOLS_MANDATE.md) ก่อนเริ่มงาน**
> กฎเหล็ก: ใช้ Tools ตรวจไฟล์จริงก่อนรายงาน / ทำงานเสร็จต้องอัปเดตเอกสารในครั้งเดียวกัน / ห้ามอ้างไฟล์ที่ไม่มีจริง / รายงานสั้นมีหลักฐาน / **ข้อ 6 (Runtime Integrity): ห้ามฉีด DLL ภายนอกที่ไม่ใช่ UPM — ใช้ไลบรารีมาตรฐาน Unity เท่านั้น**

---

## 1. PROJECT OVERVIEW

CueStrike VR Billiards เป็นโปรเจกต์บิลเลียด VR ระดับ AAA บน Unity  Editor คือ 6000.4.4f1 สำหรับ Meta Quest 2/3

### Supported Game Modes
- Snooker
- 8-Ball
- 9-Ball
- Chinese 8-Ball (中式八球)

### Core Features
- MR Passthrough (Mixed Reality)
- Multiplayer via Normcore
- AI Referee — ลุงโน๊ก (Uncle Nok)
- Mascot — โบ (Bo)
- Crowd System
- Stalker Mode
- RCA Controller-less Input — รองรับทั้ง Controller-less (มือเปล่า) และ Real Cue Adapter (ไม้คิวจริง)

---

## 2. ARCHITECTURE & CONVENTIONS

### File Placement
| Type | Path |
|------|------|
| C# Runtime Scripts | `Assets/CueStrike/Scripts/` |
| Editor Scripts | `Assets/CueStrike/Editor/` |
| Documentation | Project Root |
| Config Files | `Assets/CueStrike/Config/` |

### Namespace Convention
```
CueStrike.<Module>.<Submodule>
```

### Design Patterns
- **Managers:** Singleton Pattern
- **Communication:** Event-driven (`event Action<...>` + `?.Invoke()`)
- **Editor Tools:** MenuItem with 3-layer Guard (Play Mode block, Unsaved changes prompt, Wrong scene prompt)

---

## 3. PHASE STATUS

| Phase | Description | Status | Notes |
|-------|-------------|--------|-------|
| P1 | Project Setup & Core Architecture | ✅ 100% | Base framework complete |
| P2 | VR Input & Physics System | ✅ 100% | RCA controller-less ready |
| P3 | Playability (async loading, settings, ball textures) | 🔄 In Progress | LoadingScreen + TitleScene + SettingsManager done; ball textures & practice polish pending (60%) |
| P4 | AAA Polish (post-processing, replay, achievements) | ⏳ Pending | Phase 4 not started |
| P5 | Game Modes — 8-Ball & 9-Ball | ✅ 100% | Full rule implementation |
| P6 | Game Mode — Snooker | ✅ 100% | Snooker rules & scoring complete |
| P7 | Multiplayer Normcore Integration | 🔄 Partial | SDK guarded with `#if CUESTRIKE_NORMCORE`, revert complete |
| **P8** | **Chinese 8-Ball Support & UI** | **✅ 100%** | **ChinesePoolAIStrategy.cs + UI setup + Self-Test passed + duplicate file cleanup** |
| P9 | AI Referee — ลุงโน๊ก & IK Posture Assist | ✅ 100% | IK Posture Assist (`CueStrikeIKAssist.cs`) with smooth Lerp interpolation, 45° spine bend, `Reduce Motion`/`Sitting Mode` accessibility integration; Editor Tool `Tools/CueStrike/Apply/Fix Shaders and Setup IK` auto-assigns refs (cueTip/cueBall/spineBone); Pink Shader fix complete (0 `Shader.Find("Standard")` in Scripts); Code complete for `CueStrikeMascotUncleNok.cs` (577 lines), `CueStrikeMascotManager.cs`, `UncleNokReferee.cs` |
| P10 | Mascot — โบ | 🔄 30% | BoPandaBanter referenced in CrowdSystem; core banter code needs review |
| P11 | Crowd System | 🔄 30% | `Characters/CueStrikeCrowdSystem.cs` references Uncle Nok & Bo (เวอร์ชัน `MascotSystem/` ถูกลบแล้ว 2026-08-09 — ไม่มี ref); base structure ready |
| P12 | Stalker Mode | ⏳ Pending | Not started |
| P13 | MR Passthrough Polish | 🔄 20% | Basic passthrough + `MR_RCAController.cs` ready |
| P14 | Store Submission Prep | 🔄 In Progress | STORE_SUBMISSION_CHECKLIST.md created |
---

## 4. RCA SYSTEM — สถาปัตยกรรม

### 4.1 Controller-less RCA (`Assets/CueStrike/RCA/`)

ระบบ RCA (Real Cue Adapter) รองรับ 2 โหมด:
1. **Controller-less** — ใช้ XR Hands จับตำแหน่งมือเปล่า + คำนวณแนวไม้คิวจาก hand tracking
2. **Real Cue Adapter** — ใช้ไม้คิวจริงที่มี Bluetooth/USB adapter ติดอยู่ (Dummy Mode สำหรับตอนยังไม่มี hardware)

| ไฟล์ | หน้าที่ | สถานะ |
|------|--------|--------|
| `CueStrikeRCAManager.cs` | Central state machine: Idle → Calibrating → Calibrated → Tracking → Striking | ✅ |
| `CueStrikeRCACalibrator.cs` | Calibration 5 ขั้น + save/load PlayerPrefs | ✅ |
| `CueStrikeDualHandTracker.cs` | Track 2 มือผ่าน XR Hands (ต้องมี package) | ⚠️ Stub |
| `CueStrikeCuePhysicsProfile.cs` | ฟิสิกส์ไม้คิว (ScriptableObject) | ✅ |
| `CueStrikeVisualVelocityCompensation.cs` | ชดเชย visual latency | ✅ |
| `CueStrikeKalmanPredictor.cs` | Kalman filter trajectory prediction | ✅ |
| `RCA_CalibrationUI.cs` | UI สำหรับ calibration | ✅ |
| `RCA.cs` | Base class legacy | ✅ |
| `MR_RCAController.cs` | MR Passthrough placement | ✅ |
| `QuestCameraRCA.cs` | API surface สำหรับ CV/SDK integration ภายหลัง | ✅ |

### 4.2 RCA → Noir Memory Bridge (`Assets/CueStrike/Scripts/NoirMemory/RCA/`)

| ไฟล์ | หน้าที่ | สถานะ |
|------|--------|--------|
| `CueStrikeRCANoirBridge.cs` | Singleton bridge: state machine + dummy mode + events + calibration subsystem | ✅ |
| `RCANoirCalibrationData.cs` | Persist calibration data (PlayerPrefs JSON) | ✅ |

### 4.3 Integration

| ไฟล์ | หน้าที่ | สถานะ |
|------|--------|--------|
| `NoirMemoryGameController.cs` | Subscribe bridge events, validate shot | ✅ |
| `CueStrikeShotManager.cs` | Reference `public RCA rca` | ✅ |
| `CueStrikeSaveLoadManager.cs` | Save/Load RCA data | ✅ |
| `Editor/RCANoirSetup.cs` | MenuItem: Wire RCA to Noir Memory | ✅ |
| `Editor/NoirMemorySelfTest.cs` | MenuItem: Test RCA Noir Bridge | ✅ |

### 4.4 Known Limitations
1. **DualHandTracker** ต้องติดตั้ง XR Hands package ก่อนใช้งานจริง
2. **USB/serial port connection** ยังเป็น Dummy Mode — ต้องเขียนเมื่อมี hardware จริง
3. **Prefab** ยังไม่มีสำเร็จรูปสำหรับ RCA Manager + Calibrator (ต้องสร้างใน Editor)

---

## 5. COMPLETED WORK (Latest Round)

### 🔧 Git / Version Control Setup (2026-08-09, by Buffy/Freebuff)
- ✅ **`git init`** บน branch `main` — โปรเจกต์ไม่เคยมี version control มาก่อน (ตาม `implementation_plan_git_setup.md`)
- ✅ **`.gitignore`** — Unity standard (`Library/ Temp/ Logs/ UserSettings/` caches) + dev logs `*.log` + analysis artifacts (`err*.txt`, `%f`, `gold_hex*.txt`, `filelist*.txt`, `all_scripts.txt`) + `__pycache__/` + `.freebuff/` + `.agents/` + ffmpeg downloads
- ✅ **Git LFS** — 274 binary files (`*.fbx *.obj *.png *.webm *.dll *.wav *.mp3 *.mp4 *.jpg *.jpeg`) เก็บเป็น pointer (Assets รวม 328MB)
- ✅ **SECURITY:** `api  key  ai audio/` (ElevenLabs + Stability.ai keys แบบ plaintext) ถูก exclude จาก VCS — **แนะนำพี่โม่งย้าย keys ออกนอกโปรเจกต์และ rotate**
- ✅ **Baseline commit `8f7b347`** — 8,309 files (284 C# scripts, editor tooling, Blender scripts, docs)
- ⚠️ **ยังไม่ทำ:** nested `CueStrike_Project/` skeleton (โฟลเดอร์ว่าง 0 ไฟล์ — git ไม่ track), parent `UnityProjects/CueStrike/` = สำเนาเก่า (มีโฟลเดอร์ซ้ำ `Assets/ Library/ ProjectSettings/`) รอการรวมเป็น root เดียว
- 📝 Plan: `implementation_plan_git_setup.md` | ✅ ไม่แตะโค้ด C# — compile ไม่กระทบ

### 🎬 Scene Loading Fix (2026-08-09, by Buffy/Freebuff)
- ✅ **`EditorBuildSettings.asset` มีครบ 11 scenes** (เดิมมีแค่ `Title_NoksGrandHall` — การโหลด MainMenu/ห้อง/demo ในบิลด์จะพัง) — ผ่าน `SceneBuildSettingsFixer.cs` (Editor Tool + batchmode `-executeMethod` ตามกฎข้อ 4)
- ✅ **`MainMenuUIController.cs:125` แก้ `LoadScene("hub")` → `LoadScene("Snooker_Demo")`** — scene `hub` ไม่มีอยู่จริง (ROADMAP เคยสั่งลบ/ทำใหม่) → กด Practice ใน Main Menu เดิมจะ error ทันที
- ✅ **Compile batchmode: 0 errors** (exit 0, `compile_check_buffy.log`)
- ✅ Editor Tool ใหม่: `Tools → CueStrike → Fix → Add All Scenes to Build Settings` (guard 3 ชั้น + ใช้งาน batchmode ได้)
- 📝 Plan: `implementation_plan_scene_fix.md`
- ⚠️ Note: Unity เติม `AudioImporter` block ให้ .meta ไฟล์เสียง (baseline เดิมไม่สมบูรณ์) — revert ไว้ รอ decision พี่โม่งว่าจะ commit หรือไม่

### 🧹 Duplicate Cleanup (2026-08-09, by Buffy/Freebuff — per implementation_plan_cleanup_duplicates.md)
- ✅ Verify reference ทุกคู่ (กฎข้อ 1): code refs + GUID ใน prefab/scene/asset + namespace ของ caller
- ✅ **ลบ 4 ไฟล์ + .meta** (พิสูจน์แล้วว่าไม่มี ref ใดๆ):
  - `RCA/UnityEngine.XR.Hands.cs` (0 ไบต์ — XR Hands จริง 1.5.0 อยู่ใน manifest แล้ว)
  - `MascotSystem/CueStrikeCrowdSystem.cs` (395L — MascotManager ใช้เวอร์ชัน `Characters/` แบบ same-namespace; ตัว MascotSystem ไม่มี ref + ไม่มี `CrowdReactionType`)
  - `Scripts/Multiplayer/Normcore/CueStrikeBallSync.cs` + `CueStrikeGameSync.cs` (ไม่มี ref ภายนอก + ไม่มี `#if CUESTRIKE_NORMCORE` guard — ผิดกฎข้อ 4; เก็บเวอร์ชัน guarded ใน `Multiplayer/` ไว้)
- ✅ **เก็บ 2 คู่** (พิสูจน์ว่าไม่ใช่ duplicate แท้ — ต่าง namespace, ต่าง consumer, อ้างถึงจริง): `CueStrikeNormcoreManager` (A←MultiplayerSetup, B←NormcoreSetup/SelfTests) + `ChinesePoolCallShotUI` (A←GameManager, B←UIManager+2 scenes)
- ⚠️ self-test menus `Tools/CueStrike/Debug/Test Ball Sync` + `Test Game Sync` ถูกลบตามไฟล์ — ทดแทนด้วย `MultiplayerSelfTest.cs`/`IntegrationSelfTest.cs`
- ⚠️ Note: `ChinesePoolGameManager.FindFirstObjectByType<ChinesePoolCallShotUI>()` (ns Gameplay) จะหาเวอร์ชัน UI ในฉากไม่เจอ — รอ unified UI เป็นงานต่อไป
- ✅ **Compile batchmode: 0 errors** (`compile_check_buffy.log`)

### 🚦 Automated Compile Gate (2026-08-09, by Buffy/Freebuff — per implementation_plan_compile_gate.md)
- ✅ **`tools/compile_check.sh`** — รัน Unity batchmode compile check (auto-detect Unity 6000.4.4f1, `UNITY_PATH` override ได้, exit 0 = 0 errors)
- ✅ **`.githooks/pre-commit`** — บล็อก commit ที่ stage ไฟล์ `.cs` ถ้า compile ไม่ผ่าน (ข้ามได้ `git commit --no-verify`); ข้ามอัตโนมัติเมื่อไม่มี `.cs` staged หรือ Unity Editor เปิดอยู่
- ✅ **`.githooks/` versioned** — คัดลอก LFS hooks (post-checkout/commit/merge) เข้าไว้ด้วย + `git config core.hooksPath .githooks`
- ✅ **ทดสอบจริง 3 ทาง**: (a) สคริปต์ตรงๆ exit 0, (b) ไฟล์ .cs พัง → hook บล็อก (exit 1, โชว์ error CS0029), (c) ไฟล์ .cs ดี → hook ผ่าน (exit 0)
- 📝 Plan: `implementation_plan_compile_gate.md` | เป้า: ตัดวงจร compile-fix ซ้ำๆ (เคย ~40 รอบ/2 วัน จาก compile_fix_errors1-18.log)

### 🎯 ChinesePoolCallShotUI Merge (2026-08-09, by Buffy/Freebuff — per implementation_plan_merge_callshot_ui.md)
- ✅ **รวม 2 เวอร์ชัน → 1**: เก็บ `Scripts/UI/ChinesePool/ChinesePoolCallShotUI.cs` (ns `CueStrike.UI.ChinesePool`, GUID `0d69029a…` — ผูกกับ 2 scenes อยู่แล้ว); ลบ `Scripts/ChinesePool/ChinesePoolCallShotUI.cs` (ns Gameplay, 280L — **dead code**: API `ShowCallShotUI` ไม่มี caller, highlight พัง `GetBallIdFromButtonIndex`=-1, GUID ไม่มี ref)
- ✅ **แก้บั๊ก `FindFirstObjectByType` หาไม่เจอ**: `ChinesePoolGameManager.cs` เพิ่ม `using CueStrike.UI.ChinesePool;` → ค้นหา class เดียวที่เหลือ (อยู่ในฉาก) → เจอจริง (เดิมหาเวอร์ชัน Gameplay ที่ไม่มีในฉาก → null)
- ✅ GameManager + UIManager ใช้ class เดียวกันแล้ว; `CueStrikeChinesePoolRuleset.cs:266` เรียก `SetCallShot` ผ่าน GameManager — ไม่กระทบ
- ✅ **Compile batchmode: 0 errors** (`compile_check.sh` exit 0)
- ⚠️ **งานถัดไป:** `OnShotCalled` event ยังไม่มี subscriber — ควรผูก `callShotUI.OnShotCalled += SetCallShot` (+ `OnCallShotCancelled → ClearCallShot`) และ UI ในฉากบาง instance ยังมี field ว่าง (ต้อง assign + Vision audit)
- 📝 Plan: `implementation_plan_merge_callshot_ui.md`

### 🧼 Scene Name Defaults Cleanup (2026-08-09, by Buffy/Freebuff — per implementation_plan_clean_scene_names.md)
- ✅ **`TitleSceneManager.cs`**: `mainSceneName` `"MainScene"`→`"MainMenu"`, `practiceSceneName` `"PracticeHub"`→`"Snooker_Demo"`, `multiplayerSceneName`/`settingsSceneName`/`creditsSceneName` → `""` (ยังไม่มีฉาก / เป็น panel) + tooltips English ตาม convention
- ✅ Preventive — class ยังไม่ถูกใส่ใน scene ไหน (grep = 0) — ถ้าถูกใส่ในอนาคตจะไม่พัง
- ✅ **Compile batchmode: 0 errors**
- ✅ **VR Startup duplicate cleanup (R10):** เก็บ `VR/VRStartup.cs` (ตัวจริง — Quest optimization ครบ FFR/CPU-GPU/OpenXR); ลบ `Scripts/CueStrikeVRStartup.cs` (dead: GUID 0 ref, ไม่มี code ref, scene names `"Main"`/`"Boot"` ไม่มีจริง) — compile 0 errors
- 📝 Plan: `implementation_plan_clean_scene_names.md`

### 🚀 Remote + First Push (2026-08-09, by Buffy/Freebuff)
- ✅ `git remote add origin https://github.com/narisavapolk-sys/CueStrike.git` + `git push -u origin main` (7 commits, HEAD `85e6e6b`) — exit 0
- ✅ LFS 274 files / 250MB ส่งครบ (dry-run ว่าง) — GitHub free quota 1GB
- ✅ PR workflow พร้อมใช้: งานใหม่ = branch → PR → merge (ดู `TASK_PROGRESS.md` Round 9)
- ⏳ ถัดไป: GitHub Actions CI รัน compile gate ทุก PR

### 🎮 VR Startup Duplicate Cleanup (2026-08-09, by Buffy/Freebuff — per implementation_plan_vr_startup_cleanup.md)
- ✅ **เก็บ `VR/VRStartup.cs`** (`VRStartup`) — ตัวจริง: `DefaultExecutionOrder(-1000)`, auto frame rate 72/90Hz, CPU/GPU levels, FFR, OpenXR Meta Quest features config
- ✅ **ลบ `Scripts/CueStrikeVRStartup.cs`** + `.meta` — duplicate เก่า: XR-init + scene loading แต่ scene names `"Main"`/`"Boot"` ไม่มีฉากจริง (ถ้าใช้จะ error); GUID `59437c5c…` = 0 ref ทั่ว Assets
- ✅ **Compile batchmode: 0 errors** (`compile_check.sh` exit 0)
- ✅ `VRStartup.cs` ถูกใส่ในฉากแล้ว (R13 — `Boot.unity` Scene 0 + Editor tool "NARI CUE STRIKE") — เหลือ Vision audit
- 📝 Plan: `implementation_plan_vr_startup_cleanup.md`

### 🔗 Call-Shot Wiring (2026-08-09, by Buffy/Freebuff — per implementation_plan_callshot_wiring.md)
- ✅ `ChinesePoolGameManager.AutoWireReferences()`: subscribe `callShotUI.OnShotCalled += SetCallShot` + `OnCallShotCancelled += ClearCallShot` (หลังหา UI — R7 แก้ให้หาเจอแล้ว)
- ✅ `OnDestroy()`: unsubscribe ทั้งคู่ (event hygiene)
- ✅ **Compile batchmode: 0 errors**
- ⏳ ยังเหลือ: ฝั่งแสดง UI ไม่มี trigger (`ShowCallShot` ไม่มีใครเรียก) + UI ในฉากบาง instance field ว่าง (ต้อง assign + Vision audit)
- 📝 Plan: `implementation_plan_callshot_wiring.md`

### 🚦 GitHub Actions CI (2026-08-09, by Buffy/Freebuff — per implementation_plan_github_ci.md)
- ✅ **`.github/workflows/compile-gate.yml`** — trigger `pull_request` + `push` ไป main → `game-ci/unity-test-runner@v4` (editmode: Unity compile จริงก่อนรัน tests; fail = บล็อก PR) + `actions/checkout` `lfs: true` + cache `Library`
- ✅ **`.github/workflows/unity-activate.yml`** — helper สร้าง activation file (`.alf`) สำหรับตั้ง secret `UNITY_LICENSE` (รันครั้งเดียว)
- ✅ YAML valid + `.gitignore` ครอบ `*_results.xml`
- ⏳ **พี่โม่งต้องตั้ง secret `UNITY_LICENSE`** (ขั้นตอนใน `TASK_PROGRESS.md` Round 12) — workflow ครั้งแรกจะ fail จนกว่าจะตั้ง
- 📝 Plan: `implementation_plan_github_ci.md`

### 🎯 VRStartup Frame Rate Auto-Detect (2026-08-10, by Buffy/Freebuff — per implementation_plan_vrstartup_framerate.md)
**Vision audit พบ 2 bugs ใน R13 (`feat/vrstartup-menu`):**
- ❌ **Bug A:** Frame rate "auto-detect" เป็น hard-code (`Application.targetFrameRate = 90` ทุกครั้ง) — คอมเมนต์ claim "Quest 2 = 72Hz, Quest 3 = 90Hz" แต่ไม่มี detection จริง
- ❌ **Bug B:** `OnDestroy` reset guard ใช้ `gameObject.name == "[VRStartup]"` (magic name) — GameObject จริงชื่อ `BootManager` → guard ไม่เคย true

**Fix (R16, single file `Assets/CueStrike/VR/VRStartup.cs`):**
- ✅ `AutoDetectFrameRate()` ตาม `SystemInfo.deviceModel` substring: Quest 2 / Oculus Quest → 72Hz, Quest 3 / 3S / Pro → 90Hz (opt-in 120Hz บน Quest 3 เท่านั้น), default → 90Hz (PCVR/Editor/Stage)
- ✅ `DetectDeviceLabel()` ใน log line → รู้ว่า device อะไรเลย
- ✅ `s_InitInstance` (static ref) → OnDestroy track ตัว GO ที่ init ไปแล้วจริง ไม่ใช่ magic name
- ✅ New inspector field `enable120HzOnQuest3` (opt-in default false) — ไม่กระทบ existing users
- ✅ Compile gate: 0 errors
- 💡 ℹ️ Risk: runtime verification ต้องเล่นจริงใน headset เห็น Hz ว่าถูก — logged in `[VRStartup] Quest optimizations applied: 90Hz (Meta Quest 3)`.

### PlayMode & Runtime Fixes (by Dev Agent)
- ✅ EditorSceneManager guards ครบทุกไฟล์ (9 ไฟล์) — ไม่มี unguarded calls
- ✅ OptionsPanel auto-create แทน error
- ✅ Fix namespace: `CueStrikeVRInputManager` → `CueStrike.VR.Input`
- ✅ Fix NullReferenceException: `NoirMemoryResultsScreen._leaderboardData = new List<>()`
- ✅ GuardScene → no-dialog skip ใน self-tests
- ✅ **Compile: 0 Errors**

### Normcore Revert
- ✅ ลบ `CUESTRIKE_NORMCORE` define ออกจาก ProjectSettings
- ✅ ใส่ `#if` guard กลับในไฟล์ใหม่
- ✅ ไฟล์เก่า 6 ไฟล์ไม่ได้แตะ
- ✅ Unity เลิก Safe Mode

### RCA Section Added to Master Doc
- ✅ เพิ่ม Section 4: RCA SYSTEM — สถาปัตยกรรม ลง CUESTRIKE_MASTER.md
- ✅ ครอบคลุม Controller-less RCA (10 ไฟล์), Noir Memory Bridge (2 ไฟล์), Integration (5 ไฟล์)
- ✅ Known Limitations documented

### Duplicate File Cleanup
- ✅ ลบ `Assets/CueStrike/Scripts/ChinesePoolCallShotUI.cs` (duplicate) — ต้นเหตุ error CS0111 9 จุด
- ✅ ลบ `.meta` คู่กัน

### New Files Added
- `ChinesePoolAIStrategy.cs` — AI strategy for Chinese 8-Ball
- `CueStrikeBallSync.cs` — Ball synchronization
- `CueStrikeGameSync.cs` — Game state synchronization
- `STORE_SUBMISSION_CHECKLIST.md` — Store submission checklist

### Latest Round Additions (2026-08-03)
- ✅ **`Editor/CueStrikeBallTextureGenerator.cs`** — P3: ปุ่ม `CueStrike → Generate → Create Ball Textures (0-15)` สร้าง texture ลูก 16 ลูกแบบ Procedural (ไม่ต้องโหลดภายนอก)
- ✅ **`Editor/CueStrikeGrandHallSetup.cs`** — ปุ่ม `CueStrike → Setup → Grand Hall (AAA)` วางพื้นไม้ + ลุง Nok + Bo + ผู้ชม 40 (replace prefab โดยไม่ทับของเดิม)
- ✅ **`CreateSkinsAndApply.cs`** — เพิ่ม `RemoveMissingScripts()` เรียกก่อน `SaveAsPrefabAsset` ทุกจุด (ball + characters) แก้ "Error saving Prefab with missing script"
- ✅ **`บุคลิก น้องนาริ.md`** — เปลี่ยนเป็น `AI Dev Assistant Guide` (บทบาท AI ทั้งหมด) ตัดชื่อเฉพาะ "นาริ" ออก
- ✅ **Compile batchmode:** 0 CS errors (Unity 6000.4.4f1)

### Visual Fix & IK Posture Assist (per task.md)
- ✅ **Pink Shader Fix complete** — กำจัด `Shader.Find("Standard")` ทุกจุดใน `Assets/CueStrike/Scripts` และ Editor scripts (0 จุดเหลือ)
  - `RCA/CueStrikeVisualVelocityCompensation.cs` — fallback → URP/Unlit
  - `Editor/CueStrikeAAAApplyAll.cs` — fallback → URP/Unlit
  - `Editor/CueStrikeGrandHallSetup.cs` — fallback → URP/Unlit
  - `Editor/ImportBlenderAssets.cs` — fallback → URP/Unlit
- ✅ **`CueStrikeIKAssist.cs` ปรับปรุง** — เพิ่ม smooth Lerp interpolation (`Quaternion.Slerp`), 45° spine bend, trigger distance 0.5m, เชื่อมต่อ `CueStrikeAccessibilityManager` (Reduce Motion → snap, OneHanded → Sitting Mode)
- ✅ **Editor Tool `CueStrikeAAAAutomation.cs` ปรับปรุง** — `Tools/CueStrike/Apply/Fix Shaders and Setup IK` แก้สคริปต์ `Shader.Find("Standard")` ได้จริง (แก้ file บน disk), แปลง Material assets Standard → URP/Lit พร้อม property mapping, auto-assign `cueTip`/`cueBall`/`spineBone` references, 3-layer guards ครบ, Undo ได้
- ✅ **Self-Test** — `Tools/CueStrike/Test/Verify AAA Setup` ตรวจ materials + scripts + IK component references
- ✅ **Compile** — 0 errors

### BoPanda + UncleNok FBX & Prefab (2026-08-03)
- ✅ **Blender 3.6** สร้าง `BoPanda_AAA.fbx` + `UncleNok_AAA.fbx` (พร้อม `_Albedo/_Normal/_Roughness.png`) ที่ `BlenderScripts/Exports/`
- ✅ **แก้ `create_all_characters_aaa.py`** — เพิ่ม `bamboo_hat` + `bowler_hat` ใน `add_hat()`, เพิ่ม `hint_glow` + `nice_aura` ใน `add_extra_props()` ตามบุคลิกของ BoPanda (แพนด้าโบ้) และ UncleNok (ช้างลุงโน๊ก)
- ✅ **อัปเดต `create_character_aaa.py`** — เพิ่ม BoPanda + UncleNok ใน `CHARACTERS` table (ครบ 12 ตัว)
- ✅ **สร้าง `run_bopanda_unclenok.bat`** — batch file รัน Blender เฉพาะ 2 ตัวที่ขาดด้วย `CHARACTERS_ONLY`
- ✅ **คัดลอก** 8 ไฟล์ (FBX 2 + PNG 6) เข้า `Assets/CueStrike/Models/AAA_Characters/`
- ✅ **รัน Unity batch Apply All AAA** — สร้าง `Prefabs/AAA_Characters/BoPanda_AAA.prefab` + `UncleNok_AAA.prefab`
- ✅ **Self-Test: 1828 PASS, 0 FAIL** — ตัวละครครบ 12 (10 ผู้เล่น + BoPanda มา สคอต + UncleNok AI Referee)

---

## 6. KNOWN ISSUES (Pre-existing)

| Issue | Severity | Status |
|-------|----------|--------|
| Missing tags (`CueBall`, `Cushion`) | Medium | ⚠️ Pre-existing |
| CS0618 Deprecation Warnings | Low | ⚠️ Pre-existing |
| XR Controller null ตอน Start (timing) | Medium | ⚠️ Pre-existing |

> **Note:** ปัญหาเหล่านี้เป็น pre-existing ไม่ได้เกิดจากการแก้ไขรอบล่าสุด ไม่ต้องรีบแก้ก่อนเริ่ม phase ใหม่

---

## 7. WORKFLOW RULES (Iron Rules)

### Before Modifying Code
1. อ่าน `CUESTRIKE_MASTER.md` + ดูว่าไฟล์ถูกใคร reference บ้าง
2. แก้ทุกไฟล์ที่เกี่ยวข้องในครั้งเดียว
3. รัน batchmode compile นอก Unity จนกว่าจะ 0 errors
4. ถามตัวเองก่อนส่งว่าเปิด Unity จะมี error ใหม่อีกไหม?
5. อัปเดต Master Doc ทุกครั้ง

### Unity Workflow
1. โม่ง **ปิด Unity Editor** ก่อนเสมอ
2. Dev Agent เขียนโค้ด
3. รัน batchmode compile (Unity ปิด)
4. Report ผล
5. โม่งเปิด Unity → กด Self-Test → กด Play → Approve

### Emergency Protocol
- Unity Safe Mode → ขอโม่งแคป Console errors → วิเคราะห์ → สั่ง Dev Agent สร้าง Editor Script แก้ทีเดียว → โม่งกดปุ่ม Apply → compile ผ่าน
- งานหาย/Context Reset → อ่าน `CUESTRIKE_MASTER.md` (ไฟล์เดียวจบ) → ถามโม่งว่าทำอะไรต่อ

---

## 8. PENDING WORK (Next Priorities)

### High Priority
1. **Audio Assets** — ไฟล์เสียง ball-ball, ball-cushion, ball-pocket, cue-hit, ambient
2. **3D Models** — โต๊ะ, ลูกบอล, ไม้คิว, ห้อง (ตอนนี้ใช้ primitives)

### Medium Priority
3. **P9: Animator Controller + Asset Assign** — สร้าง Animator Controller สำหรับ Uncle Nok (Trigger params: Speak, Celebrate, Disappointed, Neutral, IsIdle) + assign references ใน Editor
4. **P10: Bo Panda Banter** — ตรวจสอบและเติม `BoPandaBanter.cs` ถ้ายังไม่ครบ
5. **Normcore SDK จริง** + define `CUESTRIKE_NORMCORE` (เมื่อพร้อม)
6. **Phase 3 Playability** — ball textures, practice mode polish, async loading bar UI

### Low Priority
7. **Phase 4 AAA Polish** — post-processing, replay camera, achievements, leaderboard, VR spectator
8. **P11–P13** — Crowd System, Stalker Mode, MR Polish

### Tasks พร้อมเริ่มทันที
- **P9 Animator** — Dev Agent สร้าง Animator Controller ได้เลย (ใช้ parameter ที่มีอยู่แล้วใน code)
- **P9 Reference Assign** — สร้าง Editor Tool ผูก Uncle Nok references อัตโนมัติ
- **RCA Prefab** — สร้าง Prefab สำหรับ RCA Manager + Calibrator

---

## 9. EDITOR BUTTONS (MenuItem)

### Added Tools (Automation & Asset Generation)

- **Tools/CueStrike/Create Ball Material** – Generates `BallMaterial` (URP Lit) placed in `Resources`. Used by `BallMaterialAssigner` to apply textures to the pool balls at runtime.
- **Tools/CueStrike/Create Lighting Profiles** – Generates eight `RoomLightingProfile` assets (`Room1Profile` … `Room8Profile`) under `Assets/CueStrike/Environment/Lighting/Profiles/`. Populate with ambient colors, intensities, and assign directional/extra lights per room.
- **Tools/CueStrike/Apply/Fix Shaders and Setup IK** – Performs the pink‑shader fix, converts any remaining Standard shaders to URP Lit, and auto‑assigns the `CueStrikeIKAssist` component (spine bone, cue tip, cue ball) on the GameObject tagged `Player`. Includes 3‑layer guard (Play‑mode block, unsaved changes prompt, wrong‑scene prompt) and Undo support.
- **Tools/CueStrike/Test/Verify AAA Setup** – Self‑test that validates:
  * All required audio clips exist.
  * `BallMaterial` is present and assigned.
  * `CueStrikeIKAssist` component is attached to the player.
  * `RoomLightingManager` has a valid `roomProfiles` array.
  * No missing scripts in the scene.

These tools streamline the finalization of **Apply ball textures + P3 remaining**, **Room Lighting Setup**, and **IK Posture Assist** tasks.

### MCP Unity Tools (AI-Driven Editor Control)
- **Tools/CueStrike/MCP Server** – Opens the MCP Server control window. Start/stop HTTP server (default port 8080), configure auth token, CORS, view registered tools and live logs.
- **Tools/CueStrike/MCP → Test Connection** – Ping the MCP server to verify it's running and responding.
- **Tools/CueStrike/MCP → Test Tools List** – List all registered MCP tools with descriptions.
- **Tools/CueStrike/MCP → Test Execute Code** – Execute sample C# code in the Unity Editor via MCP.
- **Tools/CueStrike/MCP → Test Read File** – Read a project file via MCP.
- **Tools/CueStrike/MCP → Test List Files** – List files in a directory with pattern matching.
- **Tools/CueStrike/MCP → Test Search Files** – Regex search across the codebase via MCP.
- **Tools/CueStrike/MCP → Run All Tests** – Run the full MCP test suite (connection, tools, execute, read, list, search).
- **Tools/CueStrike/MCP → Self-Test** – Comprehensive validation: server status, 5 tools, Zero Pink Policy check, Audio Links check.

ทุกครั้งที่ทำงานเสร็จต้องมีปุ่ม Apply/Auto-Fix ใน Unity Editor:
- `Fix All Compile Errors`
- `Setup [Feature]`
- `Wire All Managers`
- `Test [FeatureName]`

**Guard 3 ชั้น:** Play Mode block | Unsaved changes prompt | Wrong scene prompt  
**Features:** Undo ได้ | Log บอกว่าทำอะไร | Fail-safe ถ้า reference หาย

---

## 10. DEV AGENT'S 7 RULES

1. Never say "Done" → พูด "Ready for test"
2. Self-Test ทุกระบบ (MenuItem Debug/Test)
3. Setup Report ก่อนเคลม (What I Built, Manual Steps, Verify, Limitations)
4. One step at a time
5. Fail-Safe ทุก function
6. แยก Editor vs Runtime
7. Explain risks ก่อนเริ่ม

> **Code:** English 100% | **Talk to โม่ง:** ไทย สุภาพ อ่อนหวาน

---


---

## 11. MCP UNITY TOOLS — AI-Driven Unity Editor Control

### 11.1 Overview
Custom MCP (Model Context Protocol) Server implementation built directly into the CueStrike project. Enables AI assistants (Cline, Cursor, etc.) to directly control Unity Editor via JSON-RPC 2.0 over HTTP.

**Location:** `Assets/CueStrike/Editor/MCP/`

### 11.2 Architecture
| Component | File | Description |
|-----------|------|-------------|
| HTTP Server (JSON-RPC 2.0) | `McpServer.cs` | `InitializeOnLoad` — auto-starts on configurable port (default 8080) |
| Settings (ScriptableObject) | `McpSettings.cs` | Port, auth token, CORS, request limits, logging |
| Editor Window | `McpServerWindow.cs` | `Tools → CueStrike → MCP Server` — GUI for start/stop/config |
| Test Client | `McpTestClient.cs` | `Tools → CueStrike → MCP → Test *` — individual tool tests |
| Self-Test | *(planned, not created)* | Comprehensive MCP validation — currently use `McpTestClient.cs` → Run All Tests |

### 11.3 Registered Tools (5 Built-in)
| Tool | MCP Name | Capability |
|------|----------|------------|
| ExecuteCodeTool | `execute_code` | Run arbitrary C# in Editor context (create objects, modify components, etc.) |
| ReadFileTool | `read_file` | Read any text file in project |
| WriteFileTool | `write_file` | Write/create text files in project |
| ListFilesTool | `list_files` | List files with glob pattern |
| SearchFilesTool | `search_files` | Regex search across codebase |

### 11.4 Quick Start
1. **Open Unity** → `Tools → CueStrike → MCP Server`
2. **Configure** Port (default 8080), Auth Token (optional), CORS
3. **Click ▶ Start Server** → Console: `MCP Server started on http://localhost:8080/`
4. **Test** → `Tools → CueStrike → MCP → Run All Tests` (or Self-Test for full validation)

### 11.5 Menu Items
| Menu Path | Function |
|-----------|----------|
| `Tools → CueStrike → MCP Server` | Open server control window |
| `Tools → CueStrike → MCP → Test Connection` | Ping server |
| `Tools → CueStrike → MCP → Test Tools List` | List registered tools |
| `Tools → CueStrike → MCP → Test Execute Code` | Execute sample code |
| `Tools → CueStrike → MCP → Test Read File` | Read file via MCP |
| `Tools → CueStrike → MCP → Test List Files` | List directory |
| `Tools → CueStrike → MCP → Test Search Files` | Regex search |
| `Tools → CueStrike → MCP → Run All Tests` | Full test suite |
| `Tools → CueStrike → MCP → Self-Test` | **Comprehensive**: server, tools, Zero Pink, Audio |

### 11.6 Self-Test Validation (PLANNED — MCPSelfTest.cs not yet created)

> ⚠️ `MCPSelfTest.cs` was planned but never created. Current MCP testing uses `McpTestClient.cs` (`Tools/CueStrike/MCP/Run All Tests`). The list below is the planned validation spec.
1. MCP Server running
2. 5 tools registered
3. Settings asset exists
4. Execute Code tool works
5. Read File tool works
6. List Files tool works
7. Search Files tool works
8. **Zero Pink Policy** — No Standard/Hidden/Error shaders in scene
9. **Audio Links** — Required clips exist (ball_impact, ball_cushion, ball_pocket, cue_hit, ambient_room)

### 11.7 External AI Client (Cline, Python, etc.)
```bash
pip install mcp-cli
```
Config: `%USERPROFILE%\.mcp\config.json`
```json
{
  "serverUrl": "http://127.0.0.1:8080",
  "authToken": ""
}
```

### 11.8 Safety & Guards
- All Editor MenuItems include 3-layer guards (Play-mode block, Unsaved changes, Wrong scene)
- Undo support via `Undo.RecordObject` / `Undo.RegisterCreatedObjectUndo`
- Server requires explicit Start — no auto-execution
- Auth token optional but recommended for shared environments

---

*Document Version: 2026-08-05 | P8 = 100% | P9 = 100% (IK Assist + Shader Fix Complete) | MCP Server: Custom Implementation Complete | Self-Test: 9 checks including Zero Pink & Audio | Next: Awaiting โม่ง's decision — P3 Playability / Audio & 3D Models*

