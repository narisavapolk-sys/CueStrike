# Implementation Plan — R26: Real Mode Selection (Snooker 15/10/6 เป็นโหมดหลัก) + Runtime Rack Builder

**Author:** Buffy/Freebuff | **Date:** 2026-08-11
**Branch:** `feat/r26-mode-selection`
**Goal (coach-approved):** เลือกโหมดจริงจากเมนู — SNOOKER 15/10/6 เป็นโหมดหลัก + 8-Ball/9-Ball/Chinese Pool —
ใช้ `totalRedBalls` คุมการตั้งโต๊ะ (15 = สามเหลี่ยมเต็ม, 10 = ตัดแถวหลัง, 6 = สามเหลี่ยมเล็กสุด)

---

## Background (จากการตรวจจริง — Rule 1)

### สิ่งที่มีอยู่แล้ว
| ไฟล์ | สถานะ |
|------|--------|
| `CueStrikeWBPSRuleset.cs` | ✅ `totalRedBalls = 15` (public) + `ResetFrame()` ตั้ง `redsRemaining = totalRedBalls` — แต่**ไม่มี runtime rack builder**: ลูกแดงวางตายตัว 15 ลูก (สามเหลี่ยม 5 แถว) ใน Editor tool `CreateSnookerScene.cs` เท่านั้น |
| `CreateSnookerScene.cs` (Editor) | ✅ มี logic วางสามเหลี่ยม (rackApex + redSpacing, 5 แถว = 15 ลูก) + 6 สี + cue ball — **ลูปตายตัว ไม่ปรับตาม totalRedBalls** |
| `MainMenuUIController.cs` | ✅ `roomSelectionPanel` + `LoadRoom(sceneName)` — เลือก**ห้อง** ไม่ใช่**โหมด** |
| `TitleSceneManager.cs` | ✅ `mainSceneName = "MainMenu"` — PLAY → MainMenu |
| Build Settings | ✅ 11 ฉาก: Boot, Title, MainMenu, Snooker_Demo, 7 ห้อง (Cyberpunk/GrandArena/Industrial/Luxury/SpaceNebula/WarpFantasy/ZenDojo) |
| R25 (merged) | ✅ `ChinesePoolMatchSetupUI` (Best-of dialog) + `ChinesePoolMatchEndScreen` + practice mode |

### ช่องว่างที่ต้องเติม (R26)
1. **Runtime rack builder** — ต้องย้าย/สร้าง logic วางลูกแดงเป็นสามเหลี่ยมตาม `totalRedBalls`
   (15/10/6) ไปไว้ใน runtime (WBPS ruleset หรือ component ใหม่) เพื่อให้สลับโหมดได้จริง
2. **หน้าเลือกโหมดจริง** — เลือก Snooker 15/10/6 / 8-Ball / 9-Ball / Chinese Pool จากเมนู
   → ตั้งค่า → โหลดฉากห้องที่ถูกต้อง (ตอนนี้เลือกได้แค่ห้อง)
3. **AAA_RoomDAY ไม่อยู่ใน Build Settings** — ฉากที่เล่น ChinesePool จริงต้องถูกเพิ่มเพื่อให้โหลดได้จากเมนู

### การตัดสินใจออกแบบ (ตามแบบโค้ช)
- **Snooker 15/10/6 ต่างกันแค่จำนวนลูกแดงตอนตั้งโต๊ะ** — กติกา/คะแนนเหมือนเดิม 100%
  - 15 = สามเหลี่ยมเต็ม (5 แถว) / 10 = ตัดแถวหลัง (4 แถว) / 6 = สามเหลี่ยมเล็กสุด (3 แถว)
- **Runtime rack builder** ใน `CueStrikeWBPSRuleset` (method `SetupRack()`):
  - ลบลูกแดงเก่า (FindObjectsByType Red) → วางใหม่ตาม `totalRedBalls` ที่ตำแหน่งเดียวกับ CreateSnookerScene
  - เรียกจาก `ResetFrame()` — ทุกเฟรม/ทุกโหมดใช้ path เดียวกัน
- **โหมด selection** เป็น static (PlayerPrefs-backed) — `CueStrikeGameModeSelector`:
  - `SelectedMode` enum: Snooker15 / Snooker10 / Snooker6 / EightBall / NineBall / ChinesePool
  - `ApplyModeToScene()` — ตั้งค่า WBPS/GameManager ตามโหมดก่อนเริ่ม
- **UI เลือกโหมด** — ต่อเข้ากับ `roomSelectionPanel` ของ MainMenu (มีอยู่แล้ว):
  - เปลี่ยนจาก "เลือกห้อง" เป็น "เลือกโหมด" (โหมด → ห้องที่ถูกต้องอัตโนมัติ)
  - Snooker 15/10/6 → Snooker_Demo; ChinesePool → AAA_RoomDAY; 8-Ball/9-Ball → ฉากที่เหมาะสม
- **Fail-safe**: โหมดที่ยังไม่มีฉาก → log warning + fallback ฉากเดิม

---

## Files Changed

### 1. `Assets/CueStrike/Gameplay/CueStrikeWBPSRuleset.cs` (แก้)
- เพิ่ม `public void SetupRack()` — runtime rack builder:
  - ลบลูกแดงที่มีอยู่ (tag/name "Red_*" หรือ `SnookerBallType.Red`)
  - วางสามเหลี่ยมตาม `totalRedBalls` (15→5 แถว, 10→4 แถว, 6→3 แถว) — logic จาก CreateSnookerScene
  - วาง 6 สี + cue ball (ถ้ายังไม่มี)
- `ResetFrame()` เรียก `SetupRack()` (หลังตั้ง `redsRemaining`)

### 2. `Assets/CueStrike/Scripts/UI/CueStrikeGameModeSelector.cs` (ใหม่)
- Enum `GameMode`: Snooker15 / Snooker10 / Snooker6 / EightBall / NineBall / ChinesePool
- Static: `SelectedMode`, `GetRedBallsForMode()` (15/10/6), `ModeToSceneName()` (Snooker_Demo / AAA_RoomDAY / ...)
- `ApplyModeToScene()` — ตั้ง WBPS.totalRedBalls / ChinesePoolGameManager ตามโหมด

### 3. `Assets/CueStrike/UI/MainMenuUIController.cs` (แก้)
- เพิ่ม mode buttons → `SelectMode(GameMode)` → ตั้ง selector + `LoadRoom(ModeToSceneName())`
- รักษา fallback: ยังโหลดห้องได้ตามเดิม

### 4. `Assets/CueStrike/Editor/CueStrikeModeSelectionSetup.cs` (ใหม่)
- Editor tool: `Tools/CueStrike/Main Menu/30. Setup Mode Selection`
- ผูกปุ่มโหมดกับ `MainMenuUIController.SelectMode` (ถ้าฉากมีปุ่มอยู่) / สร้างปุ่มให้
- Idempotent + Guard + batchmode + self-test

### 5. `ProjectSettings/EditorBuildSettings.asset` (แก้)
- เพิ่ม `AAA_RoomDAY.unity` เข้า scene list (ให้โหลดจากเมนูได้)

### 6. docs
- `implementation_plan_r26_mode_selection.md`, `task.md`, `CUESTRIKE_MASTER.md`, `TASK_PROGRESS.md`

---

## Verify
- [ ] Batchmode compile: **0 errors, 0 warnings** (ใหม่)
- [ ] Scene load Snooker_Demo + AAA_RoomDAY: 0 errors
- [ ] Self-test editor tool
- [ ] (manual/Vision) เลือก Snooker 6 → เปิด Snooker_Demo → เห็นลูกแดง 6 ลูก (สามเหลี่ยมเล็ก)

## Out of Scope
- Animation / SFX / Multiplayer — R27+
- UI เลือกโหมดแบบ World-Space ลอยหน้าใน Title (R26 ต่อกับ MainMenu panel ที่มีอยู่; Title-Lobby-full จะมาใน polish)
