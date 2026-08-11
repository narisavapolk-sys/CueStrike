# Implementation Plan — R25: Match Flow (Best-of Setup + Scoreboard + WINNER Screen)

**Author:** Buffy/Freebuff | **Date:** 2026-08-11
**Branch:** `feat/r25-match-flow`
**Goal (coach-approved):** ระบบตกลงเลือก Best-of 3/5/7 กับ AI — UI Dialog หน้าต่างเลือกก่อนเริ่มเกม
(Single Frame / Best of 3 / Best of 5 / Best of 7 / Practice) → ส่งค่าเข้าสู่ GameManager เพื่อคุมจำนวนเฟรมที่จะชนะ
+ scoreboard ต่อเฟรม + WINNER screen ตอนจบแมตช์ + กลับเมนู

---

## Background (จากการตรวจจริง — Rule 1)

### สิ่งที่มีอยู่แล้ว
| ไฟล์ | สถานะ |
|------|--------|
| `ChinesePoolGameManager.cs` | ✅ `StartNewMatch(int bestOfFrames = 5)` มีอยู่แล้ว + `OnMatchOver` event + `framesWonPlayer1/2` + `EndFrame()` ตรวจ match end (framesNeeded = maxFrames/2 + 1) |
| `ChinesePoolUIManager.cs` | ✅ `OnGameOver(int, string)` มีอยู่แล้ว แต่ **ไม่มีใครเรียกจาก GameManager** (ไม่ได้ subscribe `OnMatchOver`) |
| `ChinesePoolScoreboard.cs` | ✅ มี `RegisterPottedBall` / `AddScore` / `ResetScoreboard` — แต่ **ไม่มีช่องแสดง frames won** และ `_scoreboard` ยังไม่ถูก assign ในฉาก AAA_RoomDAY |
| `Title_NoksGrandHall.unity` | ✅ Lobby — TitleSceneManager `LoadScene("MainMenu")` |
| `MainMenuUIController.cs` | ✅ `LoadRoom(sceneName)` → เปิด room scenes (AAA_RoomDAY) |
| `CueStrikePauseMenu.cs` | ✅ กลับเมนูด้วย `SceneManager.LoadScene("Title_NoksGrandHall")` |
| `AAA_RoomDAY.unity` | ✅ มี Canvas + ChinesePoolGameManager ×2 (จริงๆ 1 active) + ChinesePoolUIManager |

### ช่องว่างที่ต้องเติม (R25)
1. **UI เลือกเงื่อนไขก่อนเริ่ม** — ไม่มี panel ใดให้เลือก Single Frame / Best of 3/5/7 / Practice
2. **Practice mode** — `StartNewMatch` รองรับแค่ best-of ที่จบแมตช์; โค้ชต้องการ Practice = เล่นไปเรื่อยๆ ไม่มีจบแมตช์
3. **Scoreboard ต่อเฟรม** — `framesWonPlayer1/2` ไม่ถูกแสดงที่ไหน (Scoreboard ตามลูกที่ลงเท่านั้น)
4. **WINNER screen** — `OnMatchOver` ไม่มี subscriber → จบแมตช์แล้ว "เกิดอะไร" ไม่มี

### การตัดสินใจออกแบบ (ตามแบบโค้ช)
- **UI Dialog หน้าต่างเลือกก่อนเริ่มเกม** — World-Space VR panel ในฉากห้อง (หลังเลือกโหมด) แสดงตัวเลือก:
  Single Frame (= Best of 1) / Best of 3 / Best of 5 / Best of 7 / Practice
- **Practice** = `StartNewMatch(0)` — 0 แปลว่าไม่จบแมตช์ (เล่นไปเรื่อยๆ ต่อเฟรมไม่รู้จบ)
- **Scoreboard** — เพิ่ม `SetFrameScore(p1, p2)` + ช่องแสดง frames ใน Scoreboard ที่มีอยู่
- **WINNER screen** — component ใหม่ subscribe `OnMatchOver` → แสดง "WINNER: Player X" + ปุ่ม
  เริ่มใหม่ (Best of เดิม) / กลับเมนู (Title) — ตาม PauseMenu pattern
- **Fail-safe**: ถ้า GameManager/UIManager/Canvas หาย → ไม่ crash, log warning

---

## Files Changed

### 1. `Assets/CueStrike/Scripts/UI/ChinesePool/ChinesePoolScoreboard.cs` (แก้)
- เพิ่ม serialized fields: `_player1FramesText`, `_player2FramesText`
- เพิ่ม `public void SetFrameScore(int p1, int p2)` — อัปเดตช่อง frames
- `ResetScoreboard()` เรียก `SetFrameScore(0, 0)`

### 2. `Assets/CueStrike/Scripts/ChinesePool/ChinesePoolGameManager.cs` (แก้)
- `StartNewMatch(int bestOfFrames = 5)`:
  - `bestOfFrames <= 1` → Best of 1 (single frame) แต่ยังเดิน EndFrame/OnMatchOver logic ตามเดิม
  - `bestOfFrames == 0` → Practice mode (ไม่มี match end — `EndFrame` ไม่เช็คจบแมตช์)
- เพิ่ม field `public bool isPracticeMode` + property `public bool IsMatchOver => currentPhase == MatchOver`
- `EndFrame()`: ถ้า practice → ไม่เข้าวงจรจบแมตช์ (วนเฟรมถัดไปเรื่อยๆ)

### 3. `Assets/CueStrike/Scripts/UI/ChinesePool/ChinesePoolUIManager.cs` (แก้)
- เพิ่ม `public void SetFrameScore(int p1, int p2)` → forward ไป Scoreboard
- เพิ่ม `public void ShowMatchOver(string winnerText)` → แสดง state text + stop scoreboard

### 4. `Assets/CueStrike/Scripts/UI/ChinesePool/ChinesePoolMatchSetupUI.cs` (ใหม่)
- World-Space VR panel: Title + 5 ปุ่ม (Single Frame / Best of 3 / Best of 5 / Best of 7 / Practice)
- กดปุ่ม → `ChinesePoolGameManager.Instance.StartNewMatch(n)` (n=1/3/5/7/0) + `ChinesePoolUIManager.Instance.InitializeGame()`
- ปิด panel ตัวเอง; ถ้า GameManager หาย → warning + ปิด (fail-safe)
- สร้าง UI ด้วยโค้ด (ไม่มี dependency กับ prefab) — ตาม convention R24

### 5. `Assets/CueStrike/Scripts/UI/ChinesePool/ChinesePoolMatchEndScreen.cs` (ใหม่)
- subscribe `ChinesePoolGameManager.Instance.OnMatchOver` (ใน OnEnable/Start, unsubscribe OnDisable)
- แสดง panel: "WINNER: Player X" + ปุ่ม "เล่นอีกครั้ง" (StartNewMatch ด้วย Best of เดิม) + "กลับเมนู" (LoadScene Title)
- fail-safe ถ้า Instance หาย

### 6. `Assets/CueStrike/Editor/ChinesePoolMatchFlowSetup.cs` (ใหม่)
- Editor tool: `Tools/CueStrike/Room Scene/20. Setup Match Flow (Best-of + WINNER)`
- สร้าง/ผูก `ChinesePoolMatchSetupUI` + `ChinesePoolMatchEndScreen` ลง Canvas ในฉากที่เปิดอยู่ (AAA_RoomDAY / Title)
- Idempotent + Guard 3 ชั้น + batchmode entry + self-test

### 7. docs
- `implementation_plan_r25_match_flow.md` (นี้), `task.md`, `CUESTRIKE_MASTER.md`, `TASK_PROGRESS.md`

---

## Verify
- [ ] Batchmode compile: **0 errors, 0 warnings** (ใหม่)
- [ ] Scene load AAA_RoomDAY: 0 errors
- [ ] Self-test editor tool (ถ้ารันได้ใน batchmode)
- [ ] หมายเหตุ Vision audit (manual): เปิด Editor → ห้อง → เห็น panel เลือก → กด Best of 3 →
      เล่นจนเฟรมจบ → เห็น frame score → จบ 2 เฟรม → WINNER screen → กลับเมนู

## Out of Scope (R26+)
- เลือกโหมดเกมจริง (Snooker 15/10/6 / 8-Ball / 9-Ball / Chinese Pool) จากเมนู — R26
- Animation / SFX — R27/R30
