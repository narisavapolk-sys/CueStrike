# Implementation Plan — R41: Snooker AI Difficulty Selector UI

**วันที่:** 2026-08-12
**Branch:** `feat/r41-snooker-difficulty-ui` → `main`
**กฎ:** กฎข้อ 1 (ตรวจของจริง) + กฎข้อ 5 (plan ก่อน, docs + compile ในรอบเดียวกัน)

## หมายเหตุชื่อ R
พี่สั่งว่า "R38" แต่ **R38 ถูกใช้ไปแล้ว** (BallSetup fix = PR #33) → งานนี้คือ **R41** ตาม roadmap (เหมือน R34/R35 case)

## เป้าหมาย
เพิ่ม AI difficulty selector (Easy/Medium/Hard/Expert) ใน UI ของ Snooker — ผูกกับ `CueStrikeSnookerAIBridge.SetDifficulty`

## สถานะจริงที่ตรวจพบ (กฎข้อ 1)

| รายการ | สถานะ | หลักฐาน |
|--------|--------|---------|
| `CueStrikeSnookerAIBridge.SetDifficulty(SkillLevel)` | ✅ มี (line 98) — `difficulty = level` + log | bridge.cs |
| `SkillLevel` enum | ✅ มี (`CueStrike.AI` namespace) | CueStrikeAIController.cs:8 |
| `SnookerAI_Bridge` ใน Snooker_Demo | ✅ มี (line 4952-4969) | scene |
| **Canvas ใน Snooker_Demo** | ❌ **ไม่มีเลย** (grep=0) — ต้องสร้าง UI ใหม่ทั้งหมด | scene |
| R34 pattern (ChinesePoolMatchSetupUI) | ✅ มี — 4 ปุ่ม diff + CreateDifficultyButton + OnDifficultySelected + PlayerPrefs | ChinesePoolMatchSetupUI.cs |
| R36: bridge difficulty default | Medium (เริ่มได้) — เลือกได้จาก Inspector/SetDifficulty | bridge.cs:41 |

**ปัญหา:** Snooker AI เล่นได้ (R36) แต่ผู้เล่นเปลี่ยนระดับ AI ผ่าน UI ไม่ได้ — ต้องเข้า Inspector เอง

## งานจริง

1. **`SnookerDifficultyUI.cs`** (ใหม่, runtime): ลอก R34 pattern แต่ผูกกับ `CueStrikeSnookerAIBridge`:
   - สร้าง Canvas (ScreenSpaceOverlay) + panel + label "ระดับ AI:" + 4 ปุ่ม (Easy/Medium/Hard/Expert)
   - `OnDifficultySelected(level)` → `bridge.SetDifficulty(level)` + PlayerPrefs (fallback ถ้า bridge ยังไม่โหลด)
   - Highlight ปุ่มที่เลือก (สีต่าง) — แสดง difficulty ปัจจุบัน
   - Fail-safe: bridge null → retry หาใหม่ (เหมือน R31 bridge pattern)

2. **`SnookerDifficultyUISetup.cs`** (ใหม่, Editor): tool `Tools/CueStrike/AI/140. Setup Snooker Difficulty UI`
   - เพิ่ม `SnookerDifficultyUI` component ลง Snooker_Demo + assign bridge ref
   - Idempotent + self-test + batchmode

3. **Compile verify:** batchmode 0 errors (Library อุ่นบน main)
4. **รัน tool จริง** → Snooker_Demo มี Canvas + difficulty UI + bridge ref
5. **Verify:** scene YAML มี UI + bridge assigned + self-test + idempotency
6. **Docs:** CUESTRIKE_MASTER.md + TASK_PROGRESS.md + task.md (R41 section)
7. **Commit + push + เปิด PR** ต่อ `main` (base = `5fb82e1` รวม R40)

## ผลลัพธ์ที่คาดหวัง
- ผู้เล่นเลือก Easy/Medium/Hard/Expert ได้จากจอ Snooker โดยตรง → `bridge.SetDifficulty` ทันที + PlayerPrefs จำค่า
- Highlight ปุ่มที่เลือก — เห็นชัดว่าระดับปัจจุบันคืออะไร

## ความเสี่ยง / หมายเหตุ
- งานนี้แตะ `Snooker_Demo.unity` + ไฟล์ใหม่ 2 ตัว (1 runtime + 1 editor) + metas
- ไม่แตะโค้ด bridge (API มีแล้ว) — ไม่แตะฉากอื่น
- งาน UI ต้องมี EventSystem ใน scene (สร้างถ้าไม่มี) — ปุ่มกดได้จริง
