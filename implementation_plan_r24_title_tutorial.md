# Implementation Plan — R24: First-Time Tutorial in Title (Lobby)

**Author:** Buffy/Freebuff | **Date:** 2026-08-11
**Branch:** `feat/r24-title-tutorial`
**Goal (coach-approved):** เปิดเกม (Boot) → Title (Lobby) → ผู้เล่นครั้งแรกต้องผ่าน Tutorial
สอนจับไม้คิว/เล็ง/ยิง เบื้องต้น ก่อนเข้าเมนูหลัก; เคยเล่นแล้ว / กด Skip → เข้า Lobby ได้ทันที

---

## Background (จากการตรวจจริง — Rule 1)

### สิ่งที่มีอยู่แล้ว
| ไฟล์ | สถานะ |
|------|--------|
| `Gameplay/Tutorial/CueStrikeTutorialManager.cs` | ✅ ครบ — in-match tutorial ที่ validate การยิงจริง (ต้องมีโต๊ะ/ShotManager/WPARulesManager ในฉาก) |
| `CueStrikeTutorialSteps.cs` | ✅ ครบ — steps สำหรับ 8-Ball (10 steps) / 9-Ball (9 steps) |
| `CueStrikeTutorialProgress.cs` | ✅ ครบ — PlayerPrefs persistence (per-mode) |
| `CueStrikeTutorialOverlay.cs` / `StepUI.cs` | ✅ UI component (ยังไม่ถูกประกอบเป็น prefab ครบ) |
| `Title_NoksGrandHall.unity` | ✅ Lobby ใหญ่ (1.5MB) — มี Bo_Root, Crowd_Ring, CueStrike_UI_Canvas, Title, ChinesePool UI |
| `TitleSceneManager.cs` | มี class แต่ **ยังไม่ถูก attach ในฉากใดเลย** (grep GUID = 0) |

### ข้อจำกัด/การตัดสินใจ
- `CueStrikeTutorialManager` เป็น **in-match validation** — ต้องการโต๊ะบิลเลียด + ShotManager + กติกาในฉาก
  ซึ่ง Title/Lobby ไม่มี (Lobby มีแค่บรรยากาศ + ตัวละคร) → **ไม่เหมาะกับ first-time onboarding**
- ตามแบบโค้ช: R24 = **"สอนจับไม้คิว/วิธีเล็ง/วิธียิงเบื้องต้น"** ก่อนเข้าเมนู → เป็น instruction-based onboarding เบาๆ
  ไม่ต้อง validate การยิงจริง → สร้าง component ใหม่ `CueStrikeFirstTimeFlow.cs` (เล็ก, fail-safe)
- โค้ชย้ำ: **Title เป็นศูนย์กลาง** — หน้าจอเมนูเป็น World-Space UI ลอยหน้าผู้เล่น (ไม่สลับฉาก)
- R26 จะทำ UI เลือกโหมด (Snooker 15/10/6, 8-Ball, 9-Ball, Chinese) + Best-of/Practice — R24 ไม่แตะ

---

## Scope R24

### 1. สคริปต์ใหม่: `Assets/CueStrike/Scripts/TitleScene/CueStrikeFirstTimeFlow.cs`
- **PlayerPrefs key:** `CueStrike_FirstTimeTutorialDone` (int 0/1) — แยกจาก per-mode progress ของ TutorialManager
- **เงื่อนไขเริ่ม:** `Start()` เช็ค `PlayerPrefs.GetInt("CueStrike_FirstTimeTutorialDone", 0) == 0`
  - = 0 → แสดง onboarding panel (2–3 สไลด์: ยินดีต้อนรับ / จับไม้คิว+เล็ง / ยิง + เริ่มเล่น)
  - = 1 → ซ่อน panel ทันที (ไม่รบกวน)
- **ปุ่ม:** `Next` (สไลด์ถัดไป) + `Skip` (ข้ามทั้งหมด — ตั้ง flag ด้วย)
- **จบ:** สไลด์สุดท้าย → ปุ่ม "เริ่มเล่น" → ตั้ง flag = 1 → ซ่อน panel → เปิดใช้งานเมนูหลัก
- **Guards (fail-safe):**
  - ถ้า reference (panel/buttons/canvas) null → log warning + ไม่บล็อกเมนู
  - ถ้าเรียกซ้ำ (`IsShowing`) → return ทันที
  - กัน `PlayerPrefs` ล้มเหลว → try/catch
  - ไม่พึ่งพา `CueStrikeTutorialManager` (ทำงาน standalone)
- **World-Space VR friendly:** panel เป็น child ของ `CueStrike_UI_Canvas` ที่มีอยู่ — โค้ดไม่บังคับ render mode

### 2. Editor Tool: `Assets/CueStrike/Editor/FirstTimeTutorialSetup.cs`
- `Tools/CueStrike/Title Scene/10. Setup First-Time Tutorial` (ต่อจาก TitleSceneSetup เดิม)
- ทำงาน: สร้าง/ผูก `CueStrikeFirstTimeFlow` ลง Title scene + กัน duplicate
- Guard 3 ชั้นตาม convention (Play Mode block / Unsaved changes / Wrong scene) + Undo
- batchmode `-executeMethod` ใช้ได้ (กฎข้อ 4)

### 3. ผูกเข้าฉากจริง: `Title_NoksGrandHall.unity`
- เพิ่ม GameObject `FirstTimeTutorial` + component `CueStrikeFirstTimeFlow` (ด้วย Editor tool / YAML surgical)
- ผูก panel UI กับ Canvas เดิม

### 4. Docs
- `CUESTRIKE_MASTER.md` — เพิ่ม R24 ใน status + completed work
- `TASK_PROGRESS.md` — เพิ่ม Round 24 section + roadmap update

---

## Verify
1. Compile batchmode: **0 errors** (`tools/compile_check.sh`)
2. Static check: component ถูก attach ใน Title scene (GUID match)
3. PlayerPrefs logic: unit-simulate (ครั้งแรก → show; หลัง Skip/จบ → hide)

## Out of scope (รอบถัดไป)
- R25: Best-of 3/5/7 + Practice จบเกม flow
- R26: UI เลือกโหมดจริง (Snooker 15/10/6 / 8-Ball / 9-Ball / Chinese) จาก Lobby
- R27: Animation (Blender)
- R28: SFX จริง (รอพี่โม่งหาไฟล์)
