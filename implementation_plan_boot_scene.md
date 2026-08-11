# Implementation Plan: Boot Scene + VRStartup Editor Tool

**Date:** 2026-08-10 | **Rule 5:** Plan before action | **Rule 1:** All facts below verified with tools

## Objective
สร้าง Boot Scene (Scene 0) + Editor tool ตาม design ใน `VRStartup.cs:16`:
> "Attaches to the Boot Scene (Scene 0) via the 'NARI CUE STRIKE' editor menu."

เพื่อให้ Quest optimization (frame rate, CPU/GPU, FFR, OpenXR features) รันก่อนทุกอย่างตอน boot
และไม่ให้เกมค้างที่ฉากว่าง (Boot ต้อง transition ต่อไปยัง Title เอง)

## Facts (verified 2026-08-10)
- `VR/VRStartup.cs` — ตัวจริง (R10 เก็บไว้), `[DefaultExecutionOrder(-1000)]`, `Awake()` ทำ
  `DontDestroyOnLoad` + `ApplyQuestOptimizations()` + OpenXR feature config (editor) — **ไม่มี scene transition**
- เมนู "NARI CUE STRIKE" **ไม่เคยถูกสร้าง** (grep ทั่ว Assets เจอแค่ comment)
- Scenes อยู่ที่ `Assets/CueStrike/Scenes/` — 11 scenes, **ไม่มี Boot**
- Build settings: 11 scenes เรียง alphabetically — `AAA DAY/AAA_RoomDAY` เป็น index 0
  (เดิมก่อน R4 มีแค่ Title) → เกม build เริ่มที่ห้อง ไม่ใช่เมนู
- มี `CueStrikeLoadingScreen.LoadScene(sceneName)` (static, ns `CueStrike.VR`) — gold standard
  VR transition (progress bar + กัน freeze) — **reuse ได้** ไม่ต้องเขียน loader ใหม่
- Title scene ไฟล์ `Title_NoksGrandHall.unity` → scene name `Title_NoksGrandHall`
- Editor tool pattern: ns `CueStrike.Editor`, `[MenuItem("Tools/...")]`, 3-layer guard
  (ข้าม dialog ใน batchmode), ตาม `SceneBuildSettingsFixer.cs` (R4)

## Scope
| รายการ | รายละเอียด |
|--------|-----------|
| `VR/BootSceneLoader.cs` (ใหม่) | component เล็กๆ: `public string nextSceneName = "Title_NoksGrandHall"` → `Start()` เรียก `CueStrikeLoadingScreen.LoadScene(nextSceneName)` (guard ว่าง) |
| `Editor/BootSceneBuilder.cs` (ใหม่) | เมนู `Tools/NARI CUE STRIKE/Build Boot Scene (VRStartup)` + entry `BuildBootScene()` สำหรับ `-executeMethod` — สร้าง/บันทึก Boot.unity, ผูก VRStartup + BootSceneLoader กับ "BootManager" GO, ใส่ Boot เป็น **Scene 0** (preserve 11 ตัวเดิม), idempotent |
| `Scenes/Boot.unity` (ใหม่, เกิดจาก batchmode) | สร้างโดย Unity เอง ไม่แตะ YAML มือ |
| Build settings | 12 scenes — Boot ที่ index 0 |

**ไม่อยู่ใน scope:** ไม่แตะ `VRStartup.cs` (optimization ครบแล้ว), ไม่แตะ Title/เกมโฟลว์อื่น

## Steps
1. เขียน plan นี้ (Rule 5) ✅
2. สร้าง `BootSceneLoader.cs` (ns `CueStrike.VR`, public field ตามสไตล์ VRStartup) ✅
3. สร้าง `BootSceneBuilder.cs` (ns `CueStrike.Editor`, 3-layer guard, idempotent: ✅
   - ถ้า Boot อยู่ใน build settings แล้ว → ข้าม insert (ไม่ซ้ำ)
   - สร้าง GO "BootManager" + AddComponent<VRStartup> + AddComponent<BootSceneLoader>
   - `EditorSceneManager.SaveScene` → `Assets/CueStrike/Scenes/Boot.unity`
   - prepend `EditorBuildSettingsScene(Boot, true)` ที่ index 0
   - `AssetDatabase.SaveAssets()` + `Refresh()`
4. รัน batchmode: `-executeMethod CueStrike.Editor.BootSceneBuilder.BuildBootScene -quit` ✅
   (รอบเดียวได้ทั้งสร้าง scene + compile check ตาม Rule 4) — licensing ล่มรอบแรก (transient) → retry ผ่าน
5. Verify: ✅ Boot.unity มี VRStartup (`c650868c…`) + BootSceneLoader (`c4d6e49f…`) / build settings 12 scenes, Boot index 0 / compile 0 errors / ไม่มี .meta noise นอกงานเรา
6. อัปเดต docs (Rule 2): CUESTRIKE_MASTER.md (§5 + status) + TASK_PROGRESS.md (Round 13) + checkboxes ✅
7. Commit (gate รันอัตโนมัติ因为有 .cs) + push

## Verification (Rule 4)
- `tools/compile_check.sh` → exit 0, 0 errors (และ gate รันตอน commit)
- `EditorBuildSettings.asset` → 12 scenes, `Boot.unity` ที่ index 0
- `Boot.unity` YAML → มี component ref ของ VRStartup.cs + BootSceneLoader.cs
- `git status` → เฉพาะไฟล์งานนี้ (ระวัง .meta noise จาก Unity เหมือน R4)

## Risks / Notes
- **Loader เป็นการตัดสินใจออกแบบเพิ่ม** (design ใน comment ไม่ได้ระบุ transition) — จำเป็นเพราะ
  Boot = Scene 0 ใน build ถ้าไม่ transition เกมจะค้างฉากว่าง; reuse `CueStrikeLoadingScreen`
  (pattern มีอยู่แล้ว) + field configurable `nextSceneName` ให้ปรับได้
- Side benefit: เกม build จะเริ่มที่ Boot → Title (เมนู) แทนที่จะเริ่มที่ห้อง ตามเดิมที่ควร
- Vision audit (Rule 4) ต้องทำใน Editor โดยพี่โม่ง — batchmode ไม่เห็นภาพ
